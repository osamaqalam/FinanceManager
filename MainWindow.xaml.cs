using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using FinanceManager.Data;
using FinanceManager.Models;
using FinanceManager.Windows;
using LiveCharts;
using LiveCharts.Helpers;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly FinanceContext _context;
        private decimal _monthlyBalance;
        private int _xMin = -1;
        private int _xMax = 1;
        private PeriodUnits _period = PeriodUnits.Days;
        private DateTime _initialDateTime;
        public ObservableCollection<TransactionViewModel> RecentTransactions { get; set; }
        public SeriesCollection SeriesCollection { get; set; }
        public Separator YAxisSeparator { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        public decimal MonthlyBalance
        {
            get => _monthlyBalance;
            set
            {
                if (_monthlyBalance != value)
                {
                    _monthlyBalance = value;
                    OnPropertyChanged(nameof(MonthlyBalance));
                }
            }
        }

        public int XMin
        {
            get { return _xMin; }
            set
            {
                _xMin = value;
                OnPropertyChanged("XMin");
            }
        }

        public int XMax
        {
            get { return _xMax; }
            set
            {
                _xMax = value;
                OnPropertyChanged("XMax");
            }
        }

        public PeriodUnits Period
        {
            get { return _period; }
            set
            {
                _period = value;
                OnPropertyChanged("Period");
            }
        }

        public DateTime InitialDateTime
        {
            get { return _initialDateTime; }
            set
            {
                _initialDateTime = value;
                OnPropertyChanged("InitialDateTime");
            }
        }
        public MainWindow(FinanceContext context)
        {
            InitializeComponent();
            _context = context;
            RecentTransactions = new ObservableCollection<TransactionViewModel>();
            SeriesCollection = new SeriesCollection();
            YAxisSeparator = new Separator
            {
                Step = 100 // Set the interval for the y-axis
            };
            var now = DateTime.UtcNow;
            InitialDateTime = new DateTime(now.Year, now.Month, now.Day);
            DataContext = this;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Load data in background
                var data = await Task.Run(() => LoadData());

                // Update UI with loaded data
                UpdateUI(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
        }

        private (List<Transaction> transactions,
                 decimal monthlyBalance) LoadData()
        {
            
            // Simulate a long-running task
            System.Threading.Thread.Sleep(10000);

            // Fetch data from the database
            var transactions = _context.Transactions
                .OrderByDescending(t => t.Date)
                .ToList();

            // Calculate total balance for the current month
            var monthlyBalance = _context.Transactions
                .OrderByDescending(t => t.Date)
                .Select(t => t.MBalance)
                .FirstOrDefault();

            return (transactions, monthlyBalance); 
        }

        private void UpdateUI((List<Transaction> transactions, decimal monthlyBalance) data)
        {
            var (transactions, monthlyBalance) = data;

            // Get the current date
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            MonthlyBalance = monthlyBalance;

            // Clear the existing collection
            RecentTransactions.Clear();

            // Add the fetched transactions to the ObservableCollection
            foreach (var transaction in transactions)
            {
                RecentTransactions.Add(new TransactionViewModel
                {
                    Date = transaction.Date,
                    Description = transaction.Description,
                    Amount = transaction.Amount,
                    MBalance = transaction.MBalance
                });
            }

            var mBalances = transactions
                    .GroupBy(t => t.Date.Date) // Group by date only, ignoring time
                    .OrderBy(g => g.Key)
                    .Select(g => g.OrderBy(t => t.Date).First().MBalance)
                    .ToList();

            if (transactions.Any())
            {
                InitialDateTime = new DateTime(transactions.Last().Date.Year, transactions.Last().Date.Month, transactions.Last().Date.Day);
                XMin = (int)(startOfMonth - transactions.Last().Date).TotalDays;
                XMax = (int)(endOfMonth - transactions.Last().Date).TotalDays;
            }
            else
            {
                InitialDateTime = new DateTime(now.Year, now.Month, now.Day);
                XMin = (int)(startOfMonth - now).TotalDays + 5;
                XMax = (int)(endOfMonth - now).TotalDays + 5;
            }

            SeriesCollection.Clear();
            SeriesCollection.Add(new LineSeries
            {
                Title = "Amount",
                Values = new ChartValues<decimal>(mBalances)
            });

            DataContext = this;
        }

        private void AddTransactionButton_Click(object sender, RoutedEventArgs e)
        {
            var addTransactionWindow = new AddTransactionWindow(_context, RecentTransactions);
            addTransactionWindow.Owner = this; // Set the owner to MainWindow (optional)
            addTransactionWindow.TransactionAdded += AddTransactionWindow_TransactionAdded;
            addTransactionWindow.ShowDialog(); // Show the window as a dialog
        }

        private async void AddTransactionWindow_TransactionAdded(object sender, EventArgs e)
        {
            await LoadDataAsync();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TransactionViewModel
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public decimal MBalance { get; set; }
        public System.DateTime Date { get; set; }
    }
}
