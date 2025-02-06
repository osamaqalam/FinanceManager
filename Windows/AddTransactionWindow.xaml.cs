using FinanceManager.Data;
using FinanceManager.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

        public AddTransactionWindow(FinanceContext context)
        {
            InitializeComponent();
            _context = context; // Use the injected context
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // Example: Save a new transaction to the database
            var transaction = new Transaction
            {
                CategoryId = 1,
                Description = txtDescription.Text,
                Amount = decimal.Parse(txtAmount.Text),
                Date = DateTime.Now
            };

            _context.Transactions.Add(transaction);
            _context.SaveChanges();

            MessageBox.Show("Transaction saved!");
            this.Close(); // Close the window after saving
        }
    }
}
