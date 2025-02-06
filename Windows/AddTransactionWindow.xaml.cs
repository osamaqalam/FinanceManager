using FinanceManager.Data;
using FinanceManager.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FinanceManager.Windows
{
    /// <summary>
    /// Interaction logic for AddTransactionWindow.xaml
    /// </summary>
    public partial class AddTransactionWindow : Window
    {
        private readonly FinanceContext _context;
        private readonly ObservableCollection<TransactionViewModel> _recentTransactions;

        public AddTransactionWindow(FinanceContext context, ObservableCollection<TransactionViewModel> recentTransactions)
        {
            InitializeComponent();
            _context = context; // Use the injected context
            _recentTransactions = recentTransactions;
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // Example: Save a new transaction to the database
            var newTransaction = new Transaction
            {
                CategoryId = 1,
                Description = txtDescription.Text,
                Amount = decimal.Parse(txtAmount.Text),
                Date = DateTime.Now
            };

            _context.Transactions.Add(newTransaction);
            await _context.SaveChangesAsync();

            // Add the new transaction to the ObservableCollection
            _recentTransactions.Insert(0, new TransactionViewModel
            {
                Date = newTransaction.Date,
                Description = newTransaction.Description,
                Amount = newTransaction.Amount,
            });

            this.Close(); // Close the window after saving
        }
    }
}
