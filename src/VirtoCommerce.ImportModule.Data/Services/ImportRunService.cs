using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Notifications;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.BackgroundJobs;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public class ImportRunService : IImportRunService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserNameResolver _userNameResolver;
        private readonly IMemberService _memberService;
        private readonly IBackgroundJobExecutor _backgroundJobExecutor;
        private readonly IDataImporterFactory _dataImporterFactory;
        private readonly IPushNotificationManager _pushNotificationManager;
        private readonly INotificationSearchService _notificationSearchService;
        private readonly INotificationSender _notificationSender;
        private readonly IImportProfileCrudService _importProfileCrudService;
        private readonly IImportRunHistoryCrudService _importRunHistoryCrudService;
        private readonly IDataImportProcessManager _dataImportManager;

        public ImportRunService(
            UserManager<ApplicationUser> userManager,
            IUserNameResolver userNameResolver,
            IMemberService memberService,
            IBackgroundJobExecutor backgroundJobExecutor,
            IPushNotificationManager pushNotificationManager,
            INotificationSearchService notificationSearchService,
            INotificationSender notificationSender,
            IImportProfileCrudService importProfileCrudService,
            IImportRunHistoryCrudService importRunHistoryCrudService,
            IDataImporterFactory dataImporterFactory,
            IDataImportProcessManager dataImportManager
        )
        {
            _userManager = userManager;
            _userNameResolver = userNameResolver;
            _memberService = memberService;
            _backgroundJobExecutor = backgroundJobExecutor;
            _dataImporterFactory = dataImporterFactory;
            _pushNotificationManager = pushNotificationManager;
            _notificationSearchService = notificationSearchService;
            _notificationSender = notificationSender;
            _importProfileCrudService = importProfileCrudService;
            _importRunHistoryCrudService = importRunHistoryCrudService;
            _dataImportManager = dataImportManager;
        }

        public ImportPushNotification RunImportBackgroundJob(ImportProfile importProfile)
        {
            var pushNotification = new ImportPushNotification(_userNameResolver.GetCurrentUserName())
            {
                ProfileId = importProfile.Id,
                ProfileName = importProfile.Name
            };

            return RunImportBackgroundJob(importProfile, pushNotification);
        }

        public ImportPushNotification RunImportBackgroundJob(ImportProfile importProfile, ImportPushNotification pushNotification)
        {
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
            var pushNotification = new ImportPushNotification(_userNameResolver.GetCurrentUserName())
            {
                ProfileId = importProfile.Id,
                ProfileName = importProfile.Name
            };

            return await RunImportAsync(importProfile, pushNotification, cancellationToken);
        }

        public async Task<ImportPushNotification> RunImportAsync(ImportProfile importProfile, ImportPushNotification pushNotification, CancellationToken cancellationToken)
        {
            var profile = await _importProfileCrudService.GetByIdAsync(importProfile.Id);
            var importRunHistory = ExType<ImportRunHistory>.New().CreateNew(importProfile, pushNotification);
            async Task ProgressInfoCallback(ImportProgressInfo progressInfo)
            {
                pushNotification.Title = progressInfo.Description;

                pushNotification.EstimatingRemaining = progressInfo.EstimatingRemaining;
                pushNotification.EstimatedRemaining = progressInfo.EstimatedRemaining;

                pushNotification.ProcessedCount = progressInfo.ProcessedCount;
                pushNotification.Finished = progressInfo.Finished;
                pushNotification.TotalCount = progressInfo.TotalCount;

                pushNotification.Errors = progressInfo.Errors;
                pushNotification.ReportUrl = progressInfo.ReportUrl;

                _pushNotificationManager.Send(pushNotification);

                importRunHistory.UpdateProgress(pushNotification);
                await _importRunHistoryCrudService.SaveChangesAsync(new[] { importRunHistory });
            }
            try
            {
                importProfile.LastRun = await _importRunHistoryCrudService.GetLastRun(importProfile.UserId, importProfile.Id);               
                await _importRunHistoryCrudService.SaveChangesAsync(new[] { importRunHistory });

                await _dataImportManager.ImportAsync(importProfile, ProgressInfoCallback, cancellationToken);
            }
            catch (JobAbortedException)
            {
                pushNotification.Title = "Import was cancelled by user";
            }
            catch (Exception ex)
            {
                pushNotification.Errors.Add(ex.ToString());
                pushNotification.Title = "Import failed";
                throw;
            }
            finally
            {
                pushNotification.Finished = DateTime.UtcNow;
                
                await _pushNotificationManager.SendAsync(pushNotification);

                importRunHistory.Finish(pushNotification);
                await _importRunHistoryCrudService.SaveChangesAsync(new[] { importRunHistory });
                
                await _importProfileCrudService.SaveChangesAsync(new[] { profile });

                var user = await _userManager.FindByNameAsync(pushNotification.Creator);
                var member = await _memberService.GetByIdAsync(user.MemberId);
                var emailNotification = await _notificationSearchService.GetNotificationAsync<ImportCompletedEmailNotification>();
                emailNotification.To = user.Email;
                emailNotification.ImportRunHistory = importRunHistory;
                emailNotification.Member = member;
                await _notificationSender.ScheduleSendNotificationAsync(emailNotification);
            }

            return pushNotification;
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
