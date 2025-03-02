using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FinanceManager.Data;
using FinanceManager.Models;
using FinanceManager.Services;
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
        private readonly TransactionCategorizer _categorizer;
        private decimal _monthlyBalance;
        private int _xMin = -1;
        private int _xMax = 1;
        private decimal _yMin = -1;
        private decimal _yMax = 1;
        private const decimal Y_MARGIN = 1.1M;
        private PeriodUnits _period = PeriodUnits.Days;
        private DateTime _initialDateTime;
        public ObservableCollection<TransactionViewModel> RecentTransactions { get; set; }
        public SeriesCollection SeriesCollection { get; set; }
        public LiveCharts.Wpf.Separator YAxisSeparator { get; set; }
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
                OnPropertyChanged(nameof(XMin));
            }
        }

        public int XMax
        {
            get { return _xMax; }
            set
            {
                _xMax = value;
                OnPropertyChanged(nameof(XMax));
            }
        }

        public decimal YMin
        {
            get { return _yMin; }
            set
            {
                _yMin = value;
                OnPropertyChanged(nameof(YMin));
            }
        }

        public decimal YMax
        {
            get { return _yMax; }
            set
            {
                _yMax = value;
                OnPropertyChanged(nameof(YMax));
            }
        }

        public PeriodUnits Period
        {
            get { return _period; }
            set
            {
                _period = value;
                OnPropertyChanged(nameof(Period));
            }
        }

        public DateTime InitialDateTime
        {
            get { return _initialDateTime; }
            set
            {
                _initialDateTime = value;
                OnPropertyChanged(nameof(InitialDateTime));
            }
        }
        public MainWindow(FinanceContext context)
        {
            InitializeComponent();
            _context = context;
            _categorizer = new TransactionCategorizer("model.zip");
            RecentTransactions = new ObservableCollection<TransactionViewModel>();
            SeriesCollection = new SeriesCollection();
            YAxisSeparator = new LiveCharts.Wpf.Separator
            {
                Step = 10 // Set the interval for the y-axis
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

        private (List<TransactionViewModel> transactions, decimal monthlyBalance) LoadData()
        {
            // Get the current date
            DateTime startOfMonth = new DateTime(InitialDateTime.Year, InitialDateTime.Month, 1);
            DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // Fetch data from the database for the current month
            var transactions = (from t in _context.Transactions
                                    join c in _context.Categories
                                    on t.CategoryId equals c.CategoryId
                                    where t.Date >= startOfMonth && t.Date <= endOfMonth
                                    orderby t.Date descending
                                    select new TransactionViewModel
                                    {
                                        Date = t.Date,
                                        Description = t.Description,
                                        Amount = t.Amount,
                                        MBalance = t.MBalance,
                                        CategoryName = c.Name
                                    }).ToList();

            // Calculate total balance for the current month
            var monthlyBalance = transactions
                .OrderByDescending(t => t.Date)
                .Select(t => t.MBalance)
                .FirstOrDefault();

            return (transactions, monthlyBalance);
        }

        private void UpdateUI((List<TransactionViewModel> transactions, decimal monthlyBalance) data)
        {
            var (transactions, monthlyBalance) = data;

            MonthlyBalance = monthlyBalance;

            // Clear the existing collection
            RecentTransactions.Clear();

            // Add the fetched transactions to the ObservableCollection
            foreach (var transaction in transactions)
            {
                RecentTransactions.Add(transaction);
            }

            var mBalances = prepLineChart(transactions, monthlyBalance);

            // Managing Y-Axis scale
            if (mBalances.Any())
            {
                YMin = mBalances.Min() * Y_MARGIN;
                YMax = mBalances.Max() * Y_MARGIN;

                // Calculate the step size for approximately 5 separator lines
                decimal range = Math.Max(Math.Abs(YMin), Math.Abs(YMax));
                decimal step = range / 5;

                // Round the step size to a "pretty" number
                decimal magnitude = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)step)));
                decimal normalizedStep = step / magnitude;
                if (normalizedStep < 1.5M)
                    normalizedStep = 1;
                else if (normalizedStep < 3M)
                    normalizedStep = 2;
                else if (normalizedStep < 7M)
                    normalizedStep = 5;
                else
                    normalizedStep = 10;

                YAxisSeparator.Step = (double)(normalizedStep * magnitude);
            }

            // Get the current date
            DateTime startOfMonth = new DateTime(InitialDateTime.Year, InitialDateTime.Month, 1);
            DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // Managing X-Axis scale
            if (transactions.Any())
            {
                InitialDateTime = new DateTime(transactions.Last().Date.Year, transactions.Last().Date.Month, transactions.Last().Date.Day);
                XMin = (int)(startOfMonth - transactions.Last().Date).TotalDays;
                XMax = (int)(endOfMonth - transactions.Last().Date).TotalDays;
            }
            else
            {
                XMin = (int)(startOfMonth - InitialDateTime).TotalDays;
                XMax = (int)(endOfMonth - InitialDateTime).TotalDays;
            }

            SeriesCollection.Clear();
            SeriesCollection.Add(new LineSeries
            {
                Title = "Amount",
                Values = new ChartValues<decimal>(mBalances)
            });

            DataContext = this;
        }

        private List<decimal> prepLineChart(List<TransactionViewModel> transactions, decimal monthlyBalance)
        { 
            // Group transactions by date and get the LAST balance for each day
            var dailyBalances = transactions
                .GroupBy(t => t.Date.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(t => t.Date).Last().MBalance 
                );

            // Generate ALL DATES in the month (including days without transactions)
            var allDates = new List<DateTime>();
            for (var date = transactions.Last().Date.Date; date <= transactions.First().Date.Date; date = date.AddDays(1))
            {
                allDates.Add(date);
            }

            // Initialize with the starting monthly balance
            decimal lastMBalance = dailyBalances.Last().Value;
            var paddedBalances = new List<decimal>();

            foreach (var date in allDates)
            {
                // Update balance if transactions exist for this date
                if (dailyBalances.TryGetValue(date, out var mbalance))
                {
                    lastMBalance = mbalance;
                }

                // Add balance for this date
                paddedBalances.Add(lastMBalance);
            }

            return paddedBalances;
        }

        private void AddTransactionButton_Click(object sender, RoutedEventArgs e)
        {
            var addTransactionWindow = new AddTransactionWindow(_context, RecentTransactions);
            addTransactionWindow.Owner = this; // Set the owner to MainWindow (optional)
            addTransactionWindow.TransactionAdded += AddTransactionWindow_TransactionAdded;
            addTransactionWindow.ShowDialog(); // Show the window as a dialog
        }

        private async void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            InitialDateTime = InitialDateTime.AddMonths(-1);
            await LoadDataAsync();
        }

        private async void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            InitialDateTime = InitialDateTime.AddMonths(1);
            await LoadDataAsync();
        }

        private async void AddTransactionWindow_TransactionAdded(object sender, EventArgs e)
        {
            await LoadDataAsync();
        }

        private void MyDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditingElement is TextBox textBox)
            {
                string newCategory = textBox.Text;

                // Cast the row's data item to your model
                if (e.Row.Item is TransactionViewModel rowData)
                {
                    // Now you can access other properties of the row
                    string description = rowData.Description;

                    // For example, trigger a function with these values
                    OnRowCellValueChanged(description, newCategory);
                }
            }
        }

        private void OnRowCellValueChanged(string description, string newCategory)
        {
            TransactionData newTrainingData = new TransactionData
            {
                Description = description,
                Category = newCategory
            };

            _categorizer.AppendTrainingData(newTrainingData);
            _categorizer.RetrainModel();
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
        public string CategoryName { get; set; }
    }
}
