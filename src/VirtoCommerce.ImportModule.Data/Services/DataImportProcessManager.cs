using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Exceptions;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public class DataImportProcessManager : IDataImportProcessManager
    {
        private readonly IDataImporterFactory _dataImporterFactory;
        private readonly IImportRemainingEstimatorFactory _importRemainingEstimatorFactory;
        private readonly IImportReporterFactory _importReporterFactory;
        private readonly ISettingsManager _settingsManager;
        private readonly ILogger _logger;

        public DataImportProcessManager(
            IDataImporterFactory dataImporterFactory,
            IImportRemainingEstimatorFactory importRemainingEstimatorFactory,
            IImportReporterFactory importReporterFactory,
            ISettingsManager settingsManager,
            ILoggerFactory logger)
        {
            _dataImporterFactory = dataImporterFactory;
            _importRemainingEstimatorFactory = importRemainingEstimatorFactory;
            _importReporterFactory = importReporterFactory;
            _settingsManager = settingsManager;
            _logger = logger.CreateLogger<DataImportProcessManager>();
        }

        public async Task ImportAsync(ImportProfile importProfile, Func<ImportProgressInfo, Task> progressCallback, CancellationToken token)
        {
            var maxErrorsCountThreshold = await _settingsManager.GetValueAsync<int>(Core.ModuleConstants.Settings.General.MaxErrorsCountThreshold);

            // Create importer
            var dataImporter = _dataImporterFactory.Create(importProfile.DataImporterType);

            // Create remaining estimator
            var remainingEstimatorType = await _settingsManager.GetValueAsync<string>(Core.ModuleConstants.Settings.General.RemainingEstimator);
            var importRemainingEstimator = _importRemainingEstimatorFactory.Create(remainingEstimatorType);

            // Create reporter
            var defaultImportReporterType = await _settingsManager.GetValueAsync<string>(Core.ModuleConstants.Settings.General.DefaultImportReporter);
            var importReporterType = !string.IsNullOrEmpty(importProfile.ImportReporterType) ? importProfile.ImportReporterType : defaultImportReporterType;
            using var importReporter = _importReporterFactory.Create(importReporterType);
            importReporter.SetContext(importProfile);

            // Import progress
            var importProgress = new ImportProgressInfo
            {
                Description = "Import has been started",
            };

            var fixedSizeErrorsQueue = new FixedSizeQueue<ErrorInfo>(Math.Max(maxErrorsCountThreshold, 50));
            // Import errors
            var errorsCount = 0;
            void ErrorCallback(ErrorInfo info)
            {
                errorsCount++;
                fixedSizeErrorsQueue.Add(info);
                _logger.LogError(info.ToString());
                importProgress.Errors = fixedSizeErrorsQueue.GetTopValues().Select(x => x.ToString()).ToList();
                if (errorsCount == maxErrorsCountThreshold)
                {
                    const string limitErrorMessage = "The import process has been canceled because it exceeds the configured maximum errors limit";
                    importProgress.Errors.Add(limitErrorMessage);
                    _logger.LogError(limitErrorMessage);
                }
                progressCallback(importProgress).GetAwaiter().GetResult();
            }

            // Import context
            var context = new ImportContext(importProfile)
            {
                ProgressInfo = importProgress,
                ErrorCallback = ErrorCallback,
            };

            importRemainingEstimator.Start(context);

            await progressCallback(importProgress);

            // Reading & writing
            using var reader = await dataImporter.OpenReaderAsync(context);
            using var writer = await dataImporter.OpenWriterAsync(context);

            // Calculate total count
            importProgress.Description = "Evaluating total records counts";
            await progressCallback(importProgress);
            importProgress.TotalCount = await reader.GetTotalCountAsync(context);

            token.ThrowIfCancellationRequested();

            // Start import
            importProgress.Description = "Import in progress";

            await progressCallback(importProgress);

            try
            {
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

                    await progressCallback(importProgress);

                } while (reader.HasMoreResults && errorsCount < maxErrorsCountThreshold);
            }
            catch (Exception ex)
            {
                context.ErrorCallback?.Invoke(new ErrorInfo
                {
                    ErrorLine = context.ProgressInfo?.ProcessedCount,
                    ErrorMessage = ex.ExpandExceptionMessage(),
                });
            }
            finally
            {
                var errorReportResult = await importReporter.SaveErrorsAsync(fixedSizeErrorsQueue.GetTopValues().ToList());

                importRemainingEstimator.Stop(context);

                // Import finished
                importProgress.Description = $"Import completed {(importProgress.Errors?.Count > 0 ? "with errors" : "successfully")}";
                importProgress.Finished = DateTime.UtcNow;
                importProgress.ReportUrl = errorReportResult ?? importProgress.ReportUrl;

                await dataImporter.OnImportCompletedAsync(context);

                await progressCallback(importProgress);
            }
        }
    }
}
