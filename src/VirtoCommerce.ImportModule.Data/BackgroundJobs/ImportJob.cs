using System;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.MarketplaceVendorModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Data.Commands.RunImport;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.PushNotifications;

namespace VirtoCommerce.ImportModule.Data.BackgroundJobs
{
    public class ImportJob
    {
        private readonly IDataImportProcessManager _dataImportManager;
        private readonly IPushNotificationManager _pushNotificationManager;
        private readonly ICrudService<ImportProfile> _importProfileCrudService;

        public ImportJob(
            IDataImportProcessManager dataImportManager,
            IPushNotificationManager pushNotificationManager,
            ICrudService<ImportProfile> importProfileCrudService)
        {
            _dataImportManager = dataImportManager;
            _pushNotificationManager = pushNotificationManager;
            _importProfileCrudService = importProfileCrudService;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task ImportBackgroundAsync(RunImportCommand importDataCommand, ImportPushNotification importPushNotifaction, IJobCancellationToken token, PerformContext context)
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

            var profile = await _importProfileCrudService.GetByIdAsync(importDataCommand.ImportProfile.Id);

            profile.Run(importPushNotifaction);
            try
            {
                await _dataImportManager.ImportAsync(importDataCommand, progressInfoCallback, token.ShutdownToken);
                profile.Finish(importPushNotifaction);
            }
            catch (JobAbortedException ex)
            {
                profile.Abort(ex, importPushNotifaction);
                importPushNotifaction.Title = "Import was cancelled by user";
            }
            catch (Exception ex)
            {
                importPushNotifaction.Errors.Add(ex.ToString());
                importPushNotifaction.Title = "Import failed";
                profile.Abort(ex, importPushNotifaction);
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
