using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Hangfire;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Models.Search;
using VirtoCommerce.ImportModule.Core.Notifications;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.BackgroundJobs;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Exceptions;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Security;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public partial class ImportRunService : IImportRunService
    {
        // A job still queued or running would execute twice if requeued, and its replay would close the live run's row.
        // Deleted stays in: it is how a run is cancelled, and a cancelled run's row is resumable only once its execution
        // has saved it, so at most the closing tail (push, e-mail) is still running; ImportJob's per-profile lock holds a
        // replay behind that tail.
        private static readonly string[] _finishedJobStates = [SucceededState.StateName, FailedState.StateName, DeletedState.StateName];

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
        private readonly IImportRunHistorySearchService _importRunHistorySearchService;
        private readonly IDataImportProcessManager _dataImportManager;
        private readonly ILogger<ImportRunService> _logger;

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
            IImportRunHistorySearchService importRunHistorySearchService,
            IDataImporterFactory dataImporterFactory,
            IDataImportProcessManager dataImportManager,
            ILogger<ImportRunService> logger)
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
            _importRunHistorySearchService = importRunHistorySearchService;
            _dataImportManager = dataImportManager;
            _logger = logger;
        }

        public virtual ImportPushNotification RunImportBackgroundJob(ImportProfile importProfile)
        {
            var pushNotification = new ImportPushNotification(_userNameResolver.GetCurrentUserName())
            {
                Title = "Import process",
                ProfileId = importProfile.Id,
                ProfileName = importProfile.Name,
            };

            if (!string.IsNullOrEmpty(importProfile.ImportFileUrl))
            {
                importProfile.ImportFileUrl = Uri.UnescapeDataString(importProfile.ImportFileUrl);
            }

            return RunImportBackgroundJob(importProfile, pushNotification);
        }

        public virtual ImportPushNotification RunImportBackgroundJob(ImportProfile importProfile, ImportPushNotification pushNotification)
        {
            var jobId = _backgroundJobExecutor.Enqueue<ImportJob>(x => x.ImportBackgroundAsync(importProfile, pushNotification, JobCancellationToken.Null, null));

            pushNotification.JobId = jobId;

            return pushNotification;
        }

        public virtual void CancelRunBackgroundJob(ImportCancellationRequest cancellationRequest)
        {
            BackgroundJob.Delete(cancellationRequest.JobId);
        }

        public virtual async Task<ImportPushNotification> ResumeImportAsync(string runHistoryId)
        {
            var history = await _importRunHistoryCrudService.GetByIdAsync(runHistoryId);
            if (history is null)
            {
                throw new OperationCanceledException($"Import run history {runHistoryId} is not found");
            }
            if (!history.IsResumable())
            {
                // Also the common answer to a second resume: the replay saves the row unfinished as soon as it starts.
                throw new ValidationException([new ValidationFailure(nameof(ImportRunHistory.Cursor), $"Import run {runHistoryId} has nothing to resume: it is still running or saved no checkpoint.")
                {
                    ErrorCode = "NotResumable",
                }]);
            }
            if (string.IsNullOrEmpty(history.JobId))
            {
                throw new InvalidOperationException($"Could not re-queue import run {runHistoryId}");
            }
            if (!RequeueHangfireJob(history.JobId))
            {
                throw CreateRequeueRefusal(runHistoryId, history.JobId);
            }

            return new ImportPushNotification(_userNameResolver.GetCurrentUserName())
            {
                Title = "Import process",
                ProfileId = history.ProfileId,
                ProfileName = history.ProfileName,
                JobId = history.JobId,
                ProcessedCount = history.ProcessedCount,
                TotalCount = history.TotalCount,
                Errors = history.Errors,
                ReportUrl = history.ReportUrl,
            };
        }

        protected virtual bool RequeueHangfireJob(string jobId)
        {
            // Each attempt checks the job's state under the job's lock, so a job that starts in between is never requeued.
            return _finishedJobStates.Any(x => BackgroundJob.Requeue(jobId, x));
        }

        /// <summary>
        /// Returns the job's current Hangfire state name, or null when the job no longer exists (finished jobs expire).
        /// </summary>
        protected virtual string GetHangfireJobState(string jobId)
        {
            using var connection = JobStorage.Current.GetConnection();

            return connection.GetJobData(jobId)?.State;
        }

        private ValidationException CreateRequeueRefusal(string runHistoryId, string jobId)
        {
            var state = GetHangfireJobState(jobId);

            var failure = state is null
                ? new ValidationFailure(nameof(ImportRunHistory.JobId), $"Import run {runHistoryId} cannot be resumed: its background job {jobId} has expired. Start a new import instead.")
                {
                    ErrorCode = "JobExpired",
                }
                : new ValidationFailure(nameof(ImportRunHistory.JobId), $"Import run {runHistoryId} cannot be resumed while its background job {jobId} is {state}.")
                {
                    ErrorCode = "JobNotFinished",
                };

            return new ValidationException([failure]);
        }

        /// <summary>
        /// Attempts to locate an existing <see cref="ImportRunHistory"/> row to continue on the
        /// current run (typical after Hangfire <c>Requeue</c> — the replayed job carries the same
        /// JobId, and the prior history row still holds the saved cursor).
        /// Default behavior: search by <see cref="ImportPushNotification.JobId"/>, return the most
        /// recent row if it is resumable, clearing <c>Finished</c> so the following save paths treat it
        /// as in-progress again. Returns null for fresh runs or non-resumable history.
        /// A row that never finished is not resumed (<c>IsResumable()</c> requires <c>Finished</c>, because outside a job
        /// execution such a row may still be running). Here, inside the job, the per-profile concurrency lock of
        /// <c>ImportJob</c> (<c>DisableConcurrentExecutionForImportProfile</c>) guarantees that no other execution of
        /// it is running, so the row belongs to a worker that died: it is closed — finished, without a cursor, with an
        /// error saying so — and null is returned, so the replayed job starts a new run instead of leaving that row in
        /// progress for good. A job class without such a lock could close the row of a live run.
        /// </summary>
        protected virtual async Task<ImportRunHistory> TryGetRunHistoryAsync(ImportProfile importProfile, ImportPushNotification pushNotification)
        {
            if (string.IsNullOrEmpty(pushNotification?.JobId))
            {
                return null;
            }

            var criteria = new SearchImportRunHistoryCriteria
            {
                JobId = pushNotification.JobId,
                Take = 1,
                Sort = $"{nameof(ImportRunHistory.CreatedDate)}:desc",
            };
            var runHistory = (await _importRunHistorySearchService.SearchAsync(criteria))?.Results?.FirstOrDefault();

            if (runHistory?.IsResumable() != true)
            {
                if (runHistory is { Finished: null })
                {
                    await CloseInterruptedRunHistoryAsync(runHistory);
                }

                return null;
            }

            runHistory.Finished = null;

            return runHistory;
        }

        // The cursor is cleared because a cursor left on a row that is now Finished would make IsResumable() true;
        // resuming a killed run is deliberately not offered, so the closed row must stay non-resumable.
        private async Task CloseInterruptedRunHistoryAsync(ImportRunHistory runHistory)
        {
            runHistory.Finished = DateTime.UtcNow;
            runHistory.Cursor = null;
            // A new list, not Add: the row may be a shallow clone sharing its Errors list with a cached instance.
            // Stored errors are newest-first (see ImportErrorCollector.Seed).
            runHistory.Errors = ["The run was interrupted before it finished; its job was run again as a new run.", .. runHistory.Errors ?? []];
            runHistory.ErrorsCount = runHistory.Errors.Count;

            await _importRunHistoryCrudService.SaveChangesAsync([runHistory]);

            LogClosedInterruptedRunHistory(runHistory.Id, runHistory.JobId);
        }

        public virtual Task<ImportPushNotification> RunImportAsync(ImportProfile importProfile, CancellationToken cancellationToken)
        {
            var pushNotification = new ImportPushNotification(_userNameResolver.GetCurrentUserName())
            {
                ProfileId = importProfile.Id,
                ProfileName = importProfile.Name,
            };

            return RunImportAsync(importProfile, pushNotification, cancellationToken);
        }

        public virtual async Task<ImportPushNotification> RunImportAsync(ImportProfile importProfile, ImportPushNotification pushNotification, CancellationToken cancellationToken)
        {
            var importRunHistory = importProfile.RunHistory
                ?? await TryGetRunHistoryAsync(importProfile, pushNotification)
                ?? ExType<ImportRunHistory>.New().CreateNew(importProfile, pushNotification);

            SeedNotificationFromHistory(pushNotification, importRunHistory);

            var sendFailures = new NotificationSendFailures();
            Task ProgressInfoCallback(ImportProgressInfo info) => UpdateProgressAsync(info, pushNotification, importRunHistory, sendFailures);

            try
            {
                await _importRunHistoryCrudService.SaveChangesAsync([importRunHistory]);
                importProfile.RunHistory = importRunHistory;
                pushNotification.RunId = importRunHistory.Id;

                await _dataImportManager.ImportAsync(importProfile, ProgressInfoCallback, cancellationToken);
            }
            catch (JobAbortedException)
            {
                pushNotification.Description = "Import was cancelled by user";
            }
            catch (OperationCanceledException)
            {
                pushNotification.Description = "The operation was cancelled";
            }
            catch (Exception ex)
            {
                // The stack trace stays in the job's failed state and log, not in the row an admin reads.
                pushNotification.Errors.Add(ex.ExpandExceptionMessage());
                pushNotification.Description = "Import failed";
                throw;
            }
            finally
            {
                await FinishRunHistoryAsync(pushNotification, importRunHistory, sendFailures);
            }

            return pushNotification;
        }

        private async Task FinishRunHistoryAsync(ImportPushNotification pushNotification, ImportRunHistory importRunHistory, NotificationSendFailures sendFailures)
        {
            pushNotification.Finished ??= DateTime.UtcNow;

            // The row is the run's durable record: it is finished and saved before this notification goes out, so a
            // failing send can neither leave it unfinished nor replace the run's own exception. (When the pipeline reached
            // its final progress, that push announced the end already and still precedes the save; a run that failed
            // earlier is announced here only.)
            importRunHistory.Finish(pushNotification);

            try
            {
                await _importRunHistoryCrudService.SaveChangesAsync([importRunHistory]);
            }
            finally
            {
                await SendPushNotificationAsync(pushNotification, importRunHistory, sendFailures);

                if (sendFailures.Count > 1)
                {
                    LogSuppressedNotificationFailures(importRunHistory.Id, sendFailures.Count - 1);
                }
            }

            // Outside the finally, unlike the push: the e-mail carries the run history row, so it is sent only once the
            // finished row is saved.
            await SendCompletionEmailAsync(pushNotification, importRunHistory);
        }

        // Never throws: like the push, the e-mail is a notification — a failure is logged and neither fails the job nor
        // replaces the run's outcome.
        private async Task SendCompletionEmailAsync(ImportPushNotification pushNotification, ImportRunHistory importRunHistory)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(pushNotification.Creator);
                if (user != null)
                {
                    var emailNotification = await _notificationSearchService.GetNotificationAsync<ImportCompletedEmailNotification>();
                    emailNotification.To = user.Email;
                    emailNotification.ImportRunHistory = importRunHistory;
                    if (!string.IsNullOrEmpty(user.MemberId))
                    {
                        emailNotification.Member = await _memberService.GetByIdAsync(user.MemberId);
                    }
                    await _notificationSender.ScheduleSendNotificationAsync(emailNotification);
                }
            }
            catch (Exception ex)
            {
                LogFailedToSendCompletionEmail(ex, importRunHistory.Id);
            }
        }

        // Never throws: a failed send is counted, and the first one of the run is logged.
        private async Task SendPushNotificationAsync(ImportPushNotification pushNotification, ImportRunHistory importRunHistory, NotificationSendFailures sendFailures)
        {
            try
            {
                await _pushNotificationManager.SendAsync(pushNotification);
            }
            catch (Exception ex)
            {
                if (sendFailures.Count++ == 0)
                {
                    LogFailedToSendNotification(ex, importRunHistory.Id);
                }
            }
        }

        // A resumed run's notification starts with the errors and counts its interrupted part reported, so a failure
        // before the pipeline's first progress cannot finish the row without them.
        private static void SeedNotificationFromHistory(ImportPushNotification pushNotification, ImportRunHistory importRunHistory)
        {
            if (!string.IsNullOrEmpty(importRunHistory.Cursor))
            {
                pushNotification.Errors = [.. (importRunHistory.Errors ?? []).Where(x => x != ImportErrorCollector.LimitReachedMessage)];
                pushNotification.ProcessedCount = importRunHistory.ProcessedCount;
                pushNotification.TotalCount = importRunHistory.TotalCount;
            }
        }

        private protected async Task UpdateProgressAsync(ImportProgressInfo progressInfo, ImportPushNotification pushNotification, ImportRunHistory importRunHistory, NotificationSendFailures sendFailures)
        {
            pushNotification.Description = progressInfo.Description;
            pushNotification.EstimatingRemaining = progressInfo.EstimatingRemaining;
            pushNotification.EstimatedRemaining = progressInfo.EstimatedRemaining;
            pushNotification.ProcessedCount = progressInfo.ProcessedCount;
            pushNotification.Finished = progressInfo.Finished;
            pushNotification.TotalCount = progressInfo.TotalCount;
            pushNotification.Errors = progressInfo.Errors;
            pushNotification.ReportUrl = progressInfo.ReportUrl;

            if (pushNotification.ProcessedCount > 0 && pushNotification.Finished is null)
            {
                pushNotification.Description = pushNotification.TotalCount > 0
                    ? $"{pushNotification.ProcessedCount} of {pushNotification.TotalCount} have been imported"
                    : $"{pushNotification.ProcessedCount} have been imported";
            }

            await SendPushNotificationAsync(pushNotification, importRunHistory, sendFailures);

            importRunHistory.UpdateProgress(pushNotification);

            if (progressInfo.ShouldSaveHistory)
            {
                try
                {
                    if (!string.IsNullOrEmpty(progressInfo.Cursor))
                    {
                        importRunHistory.Cursor = progressInfo.Cursor;
                    }

                    await _importRunHistoryCrudService.SaveChangesAsync([importRunHistory]);

                    LogSavedImportRunHistoryCheckpoint(importRunHistory.Id, importRunHistory.ProcessedCount);
                }
                catch (Exception ex)
                {
                    LogFailedToSaveImportRunHistoryCheckpoint(ex, importRunHistory.Id, importRunHistory.ProcessedCount);
                }
            }
        }

        public virtual async Task<ImportDataPreview> PreviewAsync(ImportProfile importProfile)
        {
            var importer = _dataImporterFactory.Create(importProfile.DataImporterType);
            if (!string.IsNullOrEmpty(importProfile.ImportFileUrl))
            {
                importProfile.ImportFileUrl = Uri.UnescapeDataString(importProfile.ImportFileUrl);
            }

            var context = AbstractTypeFactory<ImportContext>.TryCreateInstance<ImportContext>(null, importProfile);

            var result = new ImportDataPreview();

            var validationResult = await importer.ValidateAsync(context);
            try
            {
                using var reader = await importer.OpenReaderAsync(context);

                result.TotalCount = await reader.GetTotalCountAsync(context);

                var records = new List<object>();

                do
                {
                    records.AddRange(await reader.ReadNextPageAsync(context));

                } while (reader.HasMoreResults && records.Count < importProfile.PreviewObjectCount);

                result.Records = records.Take(importProfile.PreviewObjectCount).ToArray();
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
            }
            result.Errors = validationResult.Errors;

            return result;
        }

        public virtual async Task<ValidationResult> ValidateAsync(ImportProfile importProfile)
        {
            var importer = _dataImporterFactory.Create(importProfile.DataImporterType);
            if (!string.IsNullOrEmpty(importProfile.ImportFileUrl))
            {
                importProfile.ImportFileUrl = Uri.UnescapeDataString(importProfile.ImportFileUrl);
            }

            var context = AbstractTypeFactory<ImportContext>.TryCreateInstance<ImportContext>(null, importProfile);

            var validationResult = await importer.ValidateAsync(context);

            return validationResult;
        }

        // Per-run count of push notifications that failed to send: the first failure is logged, the rest are counted, so an
        // outage that lasts the whole run logs twice instead of once per page.
        // private protected, not private: UpdateProgressAsync is private protected and takes it as a parameter.
        private protected sealed class NotificationSendFailures
        {
            public int Count { get; set; }
        }

        [LoggerMessage(LogLevel.Debug, "Saved import run history checkpoint {HistoryId} at {ProcessedCount}")]
        partial void LogSavedImportRunHistoryCheckpoint(string historyId, int processedCount);

        [LoggerMessage(LogLevel.Warning, "Closed import run history '{HistoryId}' that never finished: its job '{JobId}' was run again as a new run")]
        partial void LogClosedInterruptedRunHistory(string historyId, string jobId);

        [LoggerMessage(LogLevel.Error, "Failed to save import run history checkpoint {historyId} at {ProcessedCount}")]
        partial void LogFailedToSaveImportRunHistoryCheckpoint(Exception exception, string historyId, int processedCount);

        [LoggerMessage(LogLevel.Error, "Failed to send a push notification of import run history '{HistoryId}'; further failures of this run are counted")]
        partial void LogFailedToSendNotification(Exception exception, string historyId);

        [LoggerMessage(LogLevel.Error, "Failed to send the completion e-mail of import run history '{HistoryId}'")]
        partial void LogFailedToSendCompletionEmail(Exception exception, string historyId);

        [LoggerMessage(LogLevel.Error, "{Count} more push notifications of import run history '{HistoryId}' failed to send")]
        partial void LogSuppressedNotificationFailures(string historyId, int count);
    }
}
