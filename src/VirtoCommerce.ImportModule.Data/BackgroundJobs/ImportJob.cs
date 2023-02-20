using System;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.PushNotifications;

namespace VirtoCommerce.ImportModule.Data.BackgroundJobs
{
    public class ImportJob
    {
        private readonly IDataImportProcessManager _dataImportManager;
        private readonly IPushNotificationManager _pushNotificationManager;
        private readonly IImportProfileCrudService _importProfileCrudService;
        private readonly IImportRunHistoryCrudService _importRunHistoryCrudService;

        public ImportJob(
            IDataImportProcessManager dataImportManager,
            IPushNotificationManager pushNotificationManager,
            IImportProfileCrudService importProfileCrudService,
            IImportRunHistoryCrudService importRunHistoryCrudService
            )
        {
            _dataImportManager = dataImportManager;
            _pushNotificationManager = pushNotificationManager;
            _importProfileCrudService = importProfileCrudService;
            _importRunHistoryCrudService = importRunHistoryCrudService;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task ImportBackgroundAsync(ImportProfile importProfile, ImportPushNotification importPushNotification, IJobCancellationToken token, PerformContext context)
        {
            void progressInfoCallback(ImportProgressInfo progressInfo)
            {

                importPushNotification.JobId = context.BackgroundJob.Id;

                importPushNotification.EstimatingRemaining = progressInfo.EstimatingRemaining;
                importPushNotification.EstimatedRemaining = progressInfo.EstimatedRemaining;
                importPushNotification.ProcessedCount = progressInfo.ProcessedCount;
                importPushNotification.Finished = progressInfo.Finished;
                importPushNotification.TotalCount = progressInfo.TotalCount;
                importPushNotification.Title = progressInfo.Description;
                importPushNotification.Errors = progressInfo.Errors;
                importPushNotification.ReportUrl = progressInfo.ReportUrl;

                _pushNotificationManager.Send(importPushNotification);
            }


            try
            {
                await _dataImportManager.ImportAsync(importProfile, progressInfoCallback, token.ShutdownToken);

            }
            catch (JobAbortedException)
            {
                importPushNotification.Title = "Import was cancelled by user";
            }
            catch (Exception ex)
            {
                importPushNotification.Errors.Add(ex.ToString());
                importPushNotification.Title = "Import failed";
                throw;
            }
            finally
            {
                importPushNotification.Finished = DateTime.UtcNow;
                _pushNotificationManager.Send(importPushNotification);
                var importRunHistory = ExType<ImportRunHistory>.New().CreateNew(importProfile, importPushNotification);
                await _importRunHistoryCrudService.SaveChangesAsync(new[] { importRunHistory });
            }

        }
    }
}
