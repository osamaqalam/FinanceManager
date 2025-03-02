using Microsoft.ML.Data;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinanceManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Services
{
    // Data models used by ML.NET
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

    public class TransactionCategorizer
    {
        private readonly MLContext _mlContext;
        private ITransformer _trainedModel;
        private DataViewSchema _modelSchema;
        private readonly string _modelPath;
        private const string _trainingDataPath = "transactions.csv";

        public TransactionCategorizer(string modelPath)
        {
            _mlContext = new MLContext();
            _modelPath = modelPath;
            LoadModel();
        }

        private void LoadModel()
        {
            // Load the model from the .zip file.
            using (var fileStream = new FileStream(_modelPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                _trainedModel = _mlContext.Model.Load(fileStream, out _modelSchema);
            }
        }

        public TransactionPrediction Predict(string description)
        {
            var engine = _mlContext.Model.CreatePredictionEngine<TransactionData, TransactionPrediction>(_trainedModel);
            var input = new TransactionData { Description = description };
            return engine.Predict(input);
        }

        public void AppendTrainingData(TransactionData newTrainingData)
        {
            // Create the new row (ensure to handle commas appropriately if needed)
            string newRow = $"{newTrainingData.Description},{newTrainingData.Category}{Environment.NewLine}";

            // Append the new row to the CSV file
            File.AppendAllText(_trainingDataPath, newRow);
        }

        /// <summary>
        /// Retrain the model by combining existing data with new data.
        /// </summary>
        /// <param name="existingData">Existing training data as IDataView</param>
        /// <param name="newData">New data points for retraining</param>
        public void RetrainModel()
        {
            // Load data (assuming a CSV file with headers "Description,Category")
            IDataView dataView = _mlContext.Data.LoadFromTextFile<TransactionData>(
                "transactions.csv",
                separatorChar: ',',
                hasHeader: true);

            // Create the ML.NET pipeline.
            var pipeline = _mlContext.Transforms.Text.FeaturizeText(
                    outputColumnName: "Features",
                    inputColumnName: nameof(TransactionData.Description))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey(
                    outputColumnName: "Label",
                    inputColumnName: nameof(TransactionData.Category)))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                    labelColumnName: "Label",
                    featureColumnName: "Features"))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue(
                    outputColumnName: "PredictedLabel",
                    inputColumnName: "PredictedLabel"));

            // Train the updated model.
            _trainedModel = pipeline.Fit(dataView);

            // Optionally save the updated model.
            _mlContext.Model.Save(_trainedModel, dataView.Schema, _modelPath);
        }
    }
}
