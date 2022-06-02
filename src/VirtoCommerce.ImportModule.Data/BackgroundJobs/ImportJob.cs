using System;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.PushNotifications;

namespace VirtoCommerce.ImportModule.Data.BackgroundJobs
{
    public class ImportJob
    {
        private readonly IDataImportProcessManager _dataImportManager;
        private readonly IPushNotificationManager _pushNotificationManager;
        private readonly ICrudService<ImportProfile> _importProfileCrudService;
        private readonly ICrudService<ImportRunHistory> _importRunHistoryCrudService;

        public ImportJob(
            IDataImportProcessManager dataImportManager,
            IPushNotificationManager pushNotificationManager,
            ICrudService<ImportProfile> importProfileCrudService,
            ICrudService<ImportRunHistory> importRunHistoryCrudService
            )
        {
            _dataImportManager = dataImportManager;
            _pushNotificationManager = pushNotificationManager;
            _importProfileCrudService = importProfileCrudService;
            _importRunHistoryCrudService = importRunHistoryCrudService;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task ImportBackgroundAsync(ImportProfile importProfile, ImportPushNotification importPushNotifaction, IJobCancellationToken token, PerformContext context)
        {
            void progressInfoCallback(ImportProgressInfo progressInfo)
            {

                importPushNotifaction.JobId = context.BackgroundJob.Id;

                importPushNotifaction.Finished = progressInfo.Finished;
                importPushNotifaction.ProcessedCount = progressInfo.ProcessedCount;
                importPushNotifaction.TotalCount = progressInfo.TotalCount;
                importPushNotifaction.Title = progressInfo.Description;
                importPushNotifaction.Errors = progressInfo.Errors;

                _pushNotificationManager.Send(importPushNotifaction);
            }

            var profile = await _importProfileCrudService.GetByIdAsync(importProfile.Id);

            try
            {
                await _dataImportManager.ImportAsync(importProfile, progressInfoCallback, token.ShutdownToken);
                var importRunHistory = ExType<ImportRunHistory>.New().CreateNew(importProfile, importPushNotifaction);
                await _importRunHistoryCrudService.SaveChangesAsync(new[] { importRunHistory });

            }
            catch (JobAbortedException)
            {
                importPushNotifaction.Title = "Import was cancelled by user";
            }
            catch (Exception ex)
            {
                importPushNotifaction.Errors.Add(ex.ToString());
                importPushNotifaction.Title = "Import failed";
                throw;
            }
            finally
            {
                importPushNotifaction.Finished = DateTime.UtcNow;
                _pushNotificationManager.Send(importPushNotifaction);
                await _importProfileCrudService.SaveChangesAsync(new[] { profile });
            }

        }
    }
}
