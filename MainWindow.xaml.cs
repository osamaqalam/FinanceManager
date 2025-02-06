using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using FinanceManager.Data;
using FinanceManager.Models;
using FinanceManager.Windows;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager
{
    public partial class MainWindow : Window
    {
        private readonly FinanceContext _context;
        public decimal TotalBalance { get; set; }
        // public List<TransactionViewModel> RecentTransactions { get; set; }
        public ObservableCollection<TransactionViewModel> RecentTransactions { get; set; }

        public MainWindow(FinanceContext context)
        {
            InitializeComponent();
            _context = context;
            RecentTransactions = new ObservableCollection<TransactionViewModel>();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Fetch data from the database
            var recentTransactions = await _context.Transactions
                .OrderByDescending(t => t.Date)
                .Take(10) // Show the last 10 transactions
                .ToListAsync();

            // Calculate total balance
            TotalBalance = 0;

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

        private void AddTransactionButton_Click(object sender, RoutedEventArgs e)
        {
            var addTransactionWindow = new AddTransactionWindow(_context, RecentTransactions);
            addTransactionWindow.Owner = this; // Set the owner to MainWindow (optional)
            addTransactionWindow.ShowDialog(); // Show the window as a dialog
        }
    }

    public class TransactionViewModel
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public System.DateTime Date { get; set; }
    }
}
