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
            DataContext = this; // Set DataContext after initializing properties
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                // Get the current date
                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                // Fetch data from the database
                var recentTransactions = await _context.Transactions
                    .OrderByDescending(t => t.Date)
                    .Take(10) // Show the last 10 transactions
                    .ToListAsync();

                // Calculate total balance for the current month
                MonthlyBalance = await _context.Transactions
                    .OrderByDescending(t => t.Date)
                    .Select(t => t.MBalance)
                    .FirstOrDefaultAsync();

                // Clear the existing collection
                RecentTransactions.Clear();

                // Add the fetched transactions to the ObservableCollection
                foreach (var transaction in recentTransactions)
                {
                    RecentTransactions.Add(new TransactionViewModel
                    {
                        Date = transaction.Date,
                        Description = transaction.Description,
                        Amount = transaction.Amount,
                        MBalance = transaction.MBalance
                    });
                }

                // Prepare data for the chart
                var transactions = await _context.Transactions
                    .OrderByDescending(t => t.Date)
                    .ToListAsync();                

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
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
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
            await LoadData();
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
