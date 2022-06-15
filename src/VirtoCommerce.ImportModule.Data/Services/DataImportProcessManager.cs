using System;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Events;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public class DataImportProcessManager : IDataImportProcessManager
    {
        //TODO: move to options
        private const int _maxErrorsCountThreshold = 50;
        private readonly IDataImporterFactory _dataImporterFactory;

        public DataImportProcessManager(IDataImporterFactory dataImporterFactory)
        {
            _dataImporterFactory = dataImporterFactory;
        }

        public async Task ImportAsync(ImportProfile importProfile, Action<ImportProgressInfo> progressCallback, CancellationToken token)
        {

            var dataImporter = _dataImporterFactory.Create(importProfile.DataImporterType);

            var importProgress = new ImportProgressInfo
            {
                Description = "Import has been started"
            };

            progressCallback(importProgress);

            var errorsCount = 0;
            void errorCallback(ErrorInfo info)
            {
                errorsCount++;
                importProgress.Errors.Add(info.ToString());
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


            //suppress emitting all domain events during data importing. First of all to do not produces pushnotifcations during import process
            //TODO: remove  later and replace to special commands for import???
            using (EventSuppressor.SupressEvents())
            {
                do
                {
                    token.ThrowIfCancellationRequested();

                    var items = await reader.ReadNextPageAsync(context);

                    token.ThrowIfCancellationRequested();

                    await writer.WriteAsync(items, context);

                    importProgress.ProcessedCount += items.Length;

                    progressCallback(importProgress);

                } while (reader.HasMoreResults && errorsCount < _maxErrorsCountThreshold);
            }

            importProgress.Description = "Import has been finished";
            importProgress.Finished = DateTime.UtcNow;
            progressCallback(importProgress);
        }
    }
}
