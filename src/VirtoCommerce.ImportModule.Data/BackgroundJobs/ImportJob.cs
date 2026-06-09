using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;

namespace VirtoCommerce.ImportModule.Data.BackgroundJobs
{
    public class ImportJob
    {
        private readonly IImportRunService _importRunService;

        public ImportJob(IImportRunService importRunService)
        {
            _importRunService = importRunService;
        }

        [AutomaticRetry(Attempts = 0)]
        [DisableConcurrentExecutionForImportProfile(60)]
        public Task ImportBackgroundAsync(ImportProfile importProfile, ImportPushNotification pushNotification, IJobCancellationToken token, PerformContext context)
        {
            pushNotification.JobId = context?.BackgroundJob.Id;

            return _importRunService.RunImportAsync(importProfile, pushNotification, token.ShutdownToken);
        }
    }
}
