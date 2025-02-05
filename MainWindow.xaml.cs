using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FinanceManager.Data;
using FinanceManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager
{
    public partial class MainWindow : Window
    {
        private readonly FinanceContext _context;
        public decimal TotalBalance { get; set; }
        public List<TransactionViewModel> RecentTransactions { get; set; }

        public MainWindow(FinanceContext context)
        {
            InitializeComponent();
            _context = context;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Fetch data from the database
            var accounts = await _context.Accounts.ToListAsync();
            var recentTransactions = await _context.Transactions
                .OrderByDescending(t => t.Date)
                .Take(10) // Show the last 10 transactions
                .ToListAsync();

            // Calculate total balance
            TotalBalance = accounts.Sum(a => a.Balance);

            // Bind recent transactions to the DataGrid
            RecentTransactions = recentTransactions.Select(t => new TransactionViewModel
            {
                Date = t.Date,
                Description = t.Description,
                Amount = t.Amount,
                AccountName = accounts.FirstOrDefault(a => a.AccountId == t.AccountId)?.AccountName ?? "Unknown"
            }).ToList();
        }
    }

    public class TransactionViewModel
    {
        public string AccountName { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public System.DateTime Date { get; set; }
    }
}
