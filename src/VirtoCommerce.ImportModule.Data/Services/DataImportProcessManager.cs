using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public class DataImportProcessManager : IDataImportProcessManager
    {
        private readonly int _maxErrorsCountThreshold;
        private readonly string _remainingEstimator;
        private readonly string _defaultImportReporter;
        private readonly IDataImporterFactory _dataImporterFactory;
        private readonly IImportRemainingEstimatorFactory _importRemainingEstimatorFactory;
        private readonly IImportReporterFactory _importReporterFactory;

        public DataImportProcessManager(
            IDataImporterFactory dataImporterFactory,
            IImportRemainingEstimatorFactory importRemainingEstimatorFactory,
            IImportReporterFactory importReporterFactory,
            ISettingsManager settingsManager)
        {
            _dataImporterFactory = dataImporterFactory;
            _importRemainingEstimatorFactory = importRemainingEstimatorFactory;
            _importReporterFactory = importReporterFactory;
            _maxErrorsCountThreshold = settingsManager.GetValueAsync(Core.ModuleConstants.Settings.General.MaxErrorsCountThreshold.Name, 50).Result;
            _remainingEstimator =
                settingsManager.GetValueAsync(Core.ModuleConstants.Settings.General.RemainingEstimator.Name, nameof(DefaultRemainingEstimator)).Result;
            _defaultImportReporter =
                settingsManager.GetValueAsync(Core.ModuleConstants.Settings.General.DefaultImportReporter.Name, nameof(DefaultDataReporter)).Result;
        }

        public async Task ImportAsync(ImportProfile importProfile, Action<ImportProgressInfo> progressCallback, CancellationToken token)
        {
            // Create importer
            var dataImporter = _dataImporterFactory.Create(importProfile.DataImporterType);

            // Create remaining estimator
            var importRemainingEstimator = _importRemainingEstimatorFactory.Create(_remainingEstimator);

            // Create reporter
            var importReporterType = !string.IsNullOrEmpty(importProfile.ImportReporterType) ? importProfile.ImportReporterType : _defaultImportReporter;
            using var importReporter = _importReporterFactory.Create(importReporterType);
            importReporter.SetContext(importProfile);

            // Import progress
            var importProgress = new ImportProgressInfo
            {
                Description = "Import has been started"
            };

            // Import errors
            var errorsCount = 0;
            var errorsToSave = new List<ErrorInfo>();
            void ErrorCallback(ErrorInfo info)
            {
                errorsCount++;
                importProgress.Errors.Add(info.ToString());
                errorsToSave.Add(info);
                if (errorsCount == _maxErrorsCountThreshold)
                {
                    importProgress.Errors.Add("The import process has been canceled because it exceeds the configured maximum errors limit");
                }
                progressCallback(importProgress);
            }

            // Import context
            var context = new ImportContext(importProfile)
            {
                ProgressInfo = importProgress,
                ErrorCallback = ErrorCallback
            };

            importRemainingEstimator.Start(context);

            progressCallback(importProgress);

            // Reading & writing
            using var reader = dataImporter.OpenReader(context);
            using var writer = dataImporter.OpenWriter(context);

            // Calculate total count
            importProgress.Description = "Evaluating total records counts";
            progressCallback(importProgress);
            importProgress.TotalCount = await reader.GetTotalCountAsync(context);

            token.ThrowIfCancellationRequested();

            // Start import
            importProgress.Description = "Import in progress";

            progressCallback(importProgress);

            do
            {
                token.ThrowIfCancellationRequested();

                // Read items
                var items = await reader.ReadNextPageAsync(context);

                token.ThrowIfCancellationRequested();

                // Write items
                await writer.WriteAsync(items, context);

                // Update processed count
                importProgress.ProcessedCount += items.Length;

                // Update remaining estimation
                importRemainingEstimator.Update(context);
                importRemainingEstimator.Estimate(context);

                progressCallback(importProgress);

            } while (reader.HasMoreResults && errorsCount < _maxErrorsCountThreshold);
            
            var errorReportResult = await importReporter.SaveErrorsAsync(errorsToSave);

            importRemainingEstimator.Stop(context);

            // Import finished
            importProgress.Description = "Import has been finished";
            importProgress.Finished = DateTime.UtcNow;
            importProgress.ReportUrl = errorReportResult;
            
            progressCallback(importProgress);
        }
    }
}
