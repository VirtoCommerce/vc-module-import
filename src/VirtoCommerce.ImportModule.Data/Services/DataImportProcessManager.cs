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
        private readonly string _defaultImportReporter;
        private readonly IDataImporterFactory _dataImporterFactory;
        private readonly IImportReporterFactory _importReporterFactory;

        public DataImportProcessManager(IDataImporterFactory dataImporterFactory, IImportReporterFactory importReporterFactory, ISettingsManager settingsManager)
        {
            _dataImporterFactory = dataImporterFactory;
            _importReporterFactory = importReporterFactory;
            _maxErrorsCountThreshold = settingsManager.GetValueAsync(Core.ModuleConstants.Settings.General.MaxErrorsCountThreshold.Name, 50).Result;
            _defaultImportReporter = settingsManager.GetValueAsync(Core.ModuleConstants.Settings.General.DefaultImportReporter.Name, "DefaultDataReporter").Result;
        }

        public async Task ImportAsync(ImportProfile importProfile, Action<ImportProgressInfo> progressCallback, CancellationToken token)
        {
            var dataImporter = _dataImporterFactory.Create(importProfile.DataImporterType);

            string importReporterType = !string.IsNullOrEmpty(importProfile.ImportReporterType) ? importProfile.ImportReporterType : _defaultImportReporter;
            using var importReporter = _importReporterFactory.Create(importReporterType);
            importReporter.SetContext(importProfile);

            var importProgress = new ImportProgressInfo
            {
                Description = "Import has been started"
            };

            progressCallback(importProgress);

            var errorsCount = 0;
            List<ErrorInfo> errorsToSave = new List<ErrorInfo>();
            void errorCallback(ErrorInfo info)
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

            var context = new ImportContext(importProfile)
            {
                ProgressInfo = importProgress,
                ErrorCallback = errorCallback
            };

            using var reader = dataImporter.OpenReader(context);
            using var writer = dataImporter.OpenWriter(context);

            importProgress.Description = "Evaluating total records counts";
            progressCallback(importProgress);
            importProgress.TotalCount = await reader.GetTotalCountAsync(context);

            token.ThrowIfCancellationRequested();

            importProgress.Description = "Import in progress";
            progressCallback(importProgress);

            do
            {
                token.ThrowIfCancellationRequested();

                var items = await reader.ReadNextPageAsync(context);

                token.ThrowIfCancellationRequested();

                await writer.WriteAsync(items, context);

                importProgress.ProcessedCount += items.Length;

                progressCallback(importProgress);

            } while (reader.HasMoreResults && errorsCount < _maxErrorsCountThreshold);

            var errorReportResult = await importReporter.SaveErrorsAsync(errorsToSave);

            importProgress.Description = "Import has been finished";
            importProgress.Finished = DateTime.UtcNow;
            importProgress.ReportUrl = errorReportResult;
            progressCallback(importProgress);
        }
    }
}
