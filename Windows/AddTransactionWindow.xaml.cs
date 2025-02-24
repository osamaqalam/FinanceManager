﻿using FinanceManager.Data;
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
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace FinanceManager.Windows
{
    /// <summary>
    /// Interaction logic for AddTransactionWindow.xaml
    /// </summary>
    public partial class AddTransactionWindow : Window
    {
        private readonly FinanceContext _context;
        private readonly ObservableCollection<TransactionViewModel> _recentTransactions;
        private readonly PredictionEngine<FinanceManager.Windows.TransactionData, FinanceManager.Windows.TransactionPrediction> predictionEngine;

        public event EventHandler TransactionAdded;

        public AddTransactionWindow(FinanceContext context, ObservableCollection<TransactionViewModel> recentTransactions)
        {
            InitializeComponent();
            predictionEngine = LoadPreditionEngine("model.zip");
            _context = context; // Use the injected context
            _recentTransactions = recentTransactions;
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate the amount input
            if (!IsTextAllowed(txtAmount.Text))
            {
                MessageBox.Show("Please enter a valid amount in format of X.XX", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Get the selected item from the transaction type ListBox
            var selectedItem = (transactionType.SelectedItem as ListBoxItem)?.Content.ToString();

            // Determine the sign based on the selected transaction type
            int sign = selectedItem == "Withdraw" ? -1 : 1;

            decimal amount = sign * decimal.Parse(txtAmount.Text);

            // Calculate total balance for the current month
            // Get the current date
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            decimal mBalance = await _context.Transactions
                .Where(t => t.Date >= startOfMonth && t.Date <= endOfMonth)
                .SumAsync(t => t.Amount);

            mBalance += amount;

            var sampleTransaction = new TransactionData { Description = txtDescription.Text };
            var prediction = predictionEngine.Predict(sampleTransaction);

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == prediction.PredictedCategory);
            var categoryId = category?.CategoryId ?? 6; // Default to 6 if no category found

            // Example: Save a new transaction to the database
            var newTransaction = new Transaction
            {
                CategoryId = categoryId,
                Description = txtDescription.Text,
                Amount = amount,
                MBalance = mBalance,
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
                MBalance = newTransaction.MBalance
            });

            // Raise the TransactionAdded event
            TransactionAdded?.Invoke(this, EventArgs.Empty);

            this.Close(); // Close the window after saving
        }

        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex(@"^\d*\.?\d*$"); // Regex that matches allowed text (digits and a single optional decimal point)
            return regex.IsMatch(text);
        }

        private PredictionEngine<FinanceManager.Windows.TransactionData, FinanceManager.Windows.TransactionPrediction>
            LoadPreditionEngine(string modelFileName)
        {
            // Create MLContext
            var mlContext = new MLContext();

            // Load the model from the .zip file.
            DataViewSchema modelSchema;
            ITransformer trainedModel;
            using (var fileStream = new FileStream(modelFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                trainedModel = mlContext.Model.Load(fileStream, out modelSchema);
            }

            return mlContext.Model.CreatePredictionEngine<TransactionData, TransactionPrediction>(trainedModel);
        }

    }

    public class TransactionData
    {
        [LoadColumn(0)]
        public string Description { get; set; }

        [LoadColumn(1)]
        public string Category { get; set; }
    }

    public class TransactionPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedCategory { get; set; }
    }

}
