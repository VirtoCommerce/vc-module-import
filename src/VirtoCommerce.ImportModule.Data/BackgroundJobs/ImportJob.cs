using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Models.Search;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;

namespace VirtoCommerce.ImportModule.Data.BackgroundJobs
{
    public class ImportJob
    {
        private readonly IImportRunService _importRunService;
        private readonly IImportRunHistorySearchService _historySearch;

        public ImportJob(IImportRunService importRunService, IImportRunHistorySearchService historySearch)
        {
            _importRunService = importRunService;
            _historySearch = historySearch;
        }

        [AutomaticRetry(Attempts = 0)]
        [DisableConcurrentExecutionForImportProfile(60)]
        public async Task ImportBackgroundAsync(ImportProfile importProfile, ImportPushNotification pushNotification, IJobCancellationToken token, PerformContext context)
        {
            pushNotification.JobId = context?.BackgroundJob.Id;

            if (!string.IsNullOrEmpty(pushNotification.JobId))
            {
                var criteria = new SearchImportRunHistoryCriteria
                {
                    JobId = pushNotification.JobId,
                    Take = 1,
                    Sort = $"{nameof(ImportRunHistory.CreatedDate)}:desc",
                };

                var searchResult = await _historySearch.SearchAsync(criteria);
                var existing = searchResult?.Results?.FirstOrDefault();

                if (existing?.IsResumable() == true)
                {
                    existing.Finished = null;
                    importProfile.RunHistory = existing;
                }
            }

            await _importRunService.RunImportAsync(importProfile, pushNotification, token.ShutdownToken);
        }
    }
}
