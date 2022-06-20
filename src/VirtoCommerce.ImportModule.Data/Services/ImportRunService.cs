using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.BackgroundJobs;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public class ImportRunService : IImportRunService
    {
        private readonly IUserNameResolver _userNameResolver;
        private readonly IBackgroundJobExecutor _backgroundJobExecutor;
        private readonly IDataImporterFactory _dataImporterFactory;
        private readonly IPushNotificationManager _pushNotificationManager;
        private readonly IImportProfileCrudService _importProfileCrudService;
        private readonly IImportRunHistoryCrudService _importRunHistoryCrudService;
        private readonly IDataImportProcessManager _dataImportManager;

        public ImportRunService(IUserNameResolver userNameResolver,
            IBackgroundJobExecutor backgroundJobExecutor,
            IDataImporterFactory dataImporterFactory,
            IPushNotificationManager pushNotificationManager,
            IImportProfileCrudService importProfileCrudService,
            IImportRunHistoryCrudService importRunHistoryCrudService,
            IDataImportProcessManager dataImportManager
            )
        {
            _userNameResolver = userNameResolver;
            _backgroundJobExecutor = backgroundJobExecutor;
            _dataImporterFactory = dataImporterFactory;
            _pushNotificationManager = pushNotificationManager;
            _importProfileCrudService = importProfileCrudService;
            _importRunHistoryCrudService = importRunHistoryCrudService;
            _dataImportManager = dataImportManager;
        }

        public ImportPushNotification RunImportBackgroundJob(ImportProfile importProfile)
        {
            var pushNotification = new ImportPushNotification(_userNameResolver.GetCurrentUserName());
            pushNotification.ProfileId = importProfile.Id;
            pushNotification.ProfileName = importProfile.Name;

            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var processingSellerJob = monitoringApi.ProcessingJobs(0, int.MaxValue).Select(x => x.Value.Job)
               .Where(x => x.Type == typeof(ImportJob))
               .Select(x => x.Args.OfType<ImportProfile>())
               .Select(x => x.Any(y => y.UserId == importProfile.UserId))
               .FirstOrDefault();

            if (processingSellerJob)
            {
                throw new OperationCanceledException("Concurrent execution is limited");
            }
            var jobId = _backgroundJobExecutor.Enqueue<ImportJob>(x => x.ImportBackgroundAsync(importProfile, pushNotification, JobCancellationToken.Null, null));

            pushNotification.JobId = jobId;

            return pushNotification;
        }

        public void CancelRunBackgroundJob(ImportCancellationRequest cancellationRequest)
        {
            BackgroundJob.Delete(cancellationRequest.JobId);
        }

        public async Task<ImportPushNotification> RunImportAsync(ImportProfile importProfile, CancellationToken cancellationToken)
        {
            var importPushNotification = new ImportPushNotification(_userNameResolver.GetCurrentUserName());
            importPushNotification.ProfileId = importProfile.Id;
            importPushNotification.ProfileName = importProfile.Name;

            void progressInfoCallback(ImportProgressInfo progressInfo)
            {

                importPushNotification.Finished = progressInfo.Finished;
                importPushNotification.ProcessedCount = progressInfo.ProcessedCount;
                importPushNotification.TotalCount = progressInfo.TotalCount;
                importPushNotification.Title = progressInfo.Description;
                importPushNotification.Errors = progressInfo.Errors;

                _pushNotificationManager.Send(importPushNotification);
            }

            var profile = await _importProfileCrudService.GetByIdAsync(importProfile.Id);

            try
            {
                await _dataImportManager.ImportAsync(importProfile, progressInfoCallback, cancellationToken);
                var importRunHistory = ExType<ImportRunHistory>.New().CreateNew(importProfile, importPushNotification);
                await _importRunHistoryCrudService.SaveChangesAsync(new[] { importRunHistory });

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
                await _importProfileCrudService.SaveChangesAsync(new[] { profile });
            }

            return importPushNotification;
        }

        public async Task<ImportDataPreview> PreviewAsync(ImportProfile importProfile)
        {
            var importer = _dataImporterFactory.Create(importProfile.DataImporterType);
            var context = new ImportContext(importProfile);

            var result = new ImportDataPreview();

            var validationResult = await importer.ValidateAsync(context);
            try
            {
                using var reader = importer.OpenReader(context);

                var records = new List<object>();

                do
                {
                    records.AddRange(await reader.ReadNextPageAsync(context));

                } while (reader.HasMoreResults && records.Count < importProfile.PreviewObjectCount);

                result.TotalCount = await reader.GetTotalCountAsync(context);
                result.Records = records.Take(importProfile.PreviewObjectCount).ToArray();
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
            }
            result.Errors = validationResult.Errors;

            return result;
        }

        public async Task<ValidationResult> ValidateAsync(ImportProfile importProfile)
        {
            var importer = _dataImporterFactory.Create(importProfile.DataImporterType);
            var context = new ImportContext(importProfile);

            var validationResult = await importer.ValidateAsync(context);

            return validationResult;
        }

    }
}
