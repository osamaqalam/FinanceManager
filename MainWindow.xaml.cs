using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using FinanceManager.Data;
using FinanceManager.Models;
using FinanceManager.Windows;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly FinanceContext _context;
        private decimal _totalBalance;
        public ObservableCollection<TransactionViewModel> RecentTransactions { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        public decimal TotalBalance
        {
            get => _totalBalance;
            set
            {
                if (_totalBalance != value)
                {
                    _totalBalance = value;
                    OnPropertyChanged(nameof(TotalBalance));
                }
            }
        }

        public MainWindow(FinanceContext context)
        {
            InitializeComponent();
            _context = context;
            RecentTransactions = new ObservableCollection<TransactionViewModel>();
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
                TotalBalance = await _context.Transactions
                    .Where(t => t.Date >= startOfMonth && t.Date <= endOfMonth)
                    .SumAsync(t => t.Amount);

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
                    });
                }

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
        public System.DateTime Date { get; set; }
    }
}
