using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using VirtoCommerce.ImportModule.Core.Domains;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Models.Search;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.BackgroundJobs;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public class ImportRunService : IImportRunService
    {
        private readonly IUserNameResolver _userNameResolver;
        private readonly IBackgroundJobExecutor _backgroundJobExecutor;
        private readonly IDataImporterFactory _dataImporterFactory;
        private readonly IImportRunHistorySearchService _importRunHistorySearchService;

        public ImportRunService(IUserNameResolver userNameResolver,
            IBackgroundJobExecutor backgroundJobExecutor,
            IDataImporterFactory dataImporterFactory,
            IImportRunHistorySearchService importRunHistorySearchService
            )
        {
            _userNameResolver = userNameResolver;
            _backgroundJobExecutor = backgroundJobExecutor;
            _dataImporterFactory = dataImporterFactory;
            _importRunHistorySearchService = importRunHistorySearchService;
        }

        public ImportPushNotification RunImport(ImportProfile importProfile)
        {
            var pushNotification = new ImportPushNotification(_userNameResolver.GetCurrentUserName());
            pushNotification.ProfileId = importProfile.Id;
            pushNotification.ProfileName = importProfile.Name;

            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var processingSellerJob = monitoringApi.ProcessingJobs(0, int.MaxValue).Select(x => x.Value.Job)
               .Where(x => x.Type == typeof(ImportJob))
               .Select(x => x.Args.OfType<ImportProfile>())
               .Select(x => x.Any(y => y.SellerId == importProfile.SellerId))
               .FirstOrDefault();

            if (processingSellerJob)
            {
                throw new OperationCanceledException("Concurrent execution is limited");
            }
            var jobId = _backgroundJobExecutor.Enqueue<ImportJob>(x => x.ImportBackgroundAsync(importProfile, pushNotification, JobCancellationToken.Null, null));

            pushNotification.JobId = jobId;

            return pushNotification;
        }

        public void CancelJob(ImportCancellationRequest cancellationRequest)
        {
            BackgroundJob.Delete(cancellationRequest.JobId);
        }

        public async Task<ImportDataPreview> PreviewAsync(ImportProfile importProfile)
        {
            var importer = _dataImporterFactory.Create(importProfile.DataImporterType);
            var context = new ImportContext(importProfile);

            using var reader = importer.OpenReader(context);

            var records = new List<object>();

            do
            {
                records.AddRange(await reader.ReadNextPageAsync(context));

            } while (reader.HasMoreResults && records.Count < importProfile.PreviewObjectCount);

            var result = new ImportDataPreview
            {
                TotalCount = await reader.GetTotalCountAsync(context),
                Records = records.Take(importProfile.PreviewObjectCount).ToArray(),
            };

            return result;
        }

        public async Task<SearchImportRunHistoryResult> SearchImportRunHistoryAsync(SearchImportRunHistoryCriteria criteria)
        {
            var result = await _importRunHistorySearchService.SearchAsync(criteria);
            return result;
        }
    }
}
