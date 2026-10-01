using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Exceptions;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public partial class DataImportProcessManager : IDataImportProcessManager
    {
        private readonly IDataImporterFactory _dataImporterFactory;
        private readonly IImportRemainingEstimatorFactory _importRemainingEstimatorFactory;
        private readonly IImportReporterFactory _importReporterFactory;
        private readonly ISettingsManager _settingsManager;
        private readonly IImportRunHistoryCrudService _importRunHistoryService;
        private readonly ILogger _logger;

        public DataImportProcessManager(
            IDataImporterFactory dataImporterFactory,
            IImportRemainingEstimatorFactory importRemainingEstimatorFactory,
            IImportReporterFactory importReporterFactory,
            ISettingsManager settingsManager,
            IImportRunHistoryCrudService importRunHistoryService,
            ILoggerFactory logger)
        {
            _dataImporterFactory = dataImporterFactory;
            _importRemainingEstimatorFactory = importRemainingEstimatorFactory;
            _importReporterFactory = importReporterFactory;
            _settingsManager = settingsManager;
            _importRunHistoryService = importRunHistoryService;
            _logger = logger.CreateLogger<DataImportProcessManager>();
        }

        public async Task ImportAsync(ImportProfile importProfile, Func<ImportProgressInfo, Task> progressCallback, CancellationToken token)
        {
            var maxErrorsCountThreshold = await _settingsManager.GetValueAsync<int>(ModuleConstants.Settings.General.MaxErrorsCountThreshold);

            // Create importer
            var dataImporter = _dataImporterFactory.Create(importProfile.DataImporterType);

            // Create remaining estimator
            var remainingEstimatorType = await _settingsManager.GetValueAsync<string>(ModuleConstants.Settings.General.RemainingEstimator);
            var importRemainingEstimator = _importRemainingEstimatorFactory.Create(remainingEstimatorType);

            // Create reporter
            using var importReporter = await CreateReporterAsync(importProfile);

            // Import progress
            var importProgress = new ImportProgressInfo
            {
                Description = "Import has been started",
            };

            var errors = new ImportErrorCollector(maxErrorsCountThreshold, importProgress, _logger);

            // A resumed run keeps the errors its interrupted part reported. IsResumable() cannot key this:
            // TryGetRunHistoryAsync has already cleared Finished on the row it attaches. Seeding must precede the
            // first progressCallback, which aliases the row's Errors to ProgressInfo.Errors.
            if (HasStoredCursor(importProfile))
            {
                errors.Seed(importProfile.RunHistory.Errors);
            }

            // Import context — via AbstractTypeFactory so downstream can OverrideType with a derived context
            // and attach extra state in OnImportStartedAsync.
            var context = AbstractTypeFactory<ImportContext>.TryCreateInstance<ImportContext>(null, importProfile);
            context.ProgressInfo = importProgress;
            context.ErrorCallback = errors.Handle;

            importRemainingEstimator.Start(context);
            await progressCallback(importProgress);

            // Resolved after the first progress has put the seeded errors on the notification, and before the reader
            // opens: a setting that cannot be read fails the job without losing them, and the cursor is validated
            // against the lifetime.
            context.CursorLifetime = TimeSpan.FromDays(await GetCursorSettingAsync(importProfile, ImportCursorSettings.LifetimeDays));
            context.CursorSaveIntervalPages = await GetCursorSettingAsync(importProfile, ImportCursorSettings.SaveIntervalPages);

            // Reading & writing
            using var reader = await dataImporter.OpenReaderAsync(context);
            using var writer = await dataImporter.OpenWriterAsync(context);

            context.IsResume = await TryRestoreCursorAsync(reader, context, errors, progressCallback);

            await dataImporter.OnImportStartedAsync(context);

            // Calculate total count
            importProgress.Description = "Evaluating total records counts";
            await progressCallback(importProgress);
            importProgress.TotalCount = await reader.GetTotalCountAsync(context);

            token.ThrowIfCancellationRequested();

            // Start import
            importProgress.Description = "Import in progress";
            await progressCallback(importProgress);

            try
            {
                await ReadAndWritePagesAsync(context, reader, writer, errors, importRemainingEstimator, progressCallback, token);
            }
            catch (Exception ex)
            {
                context.ErrorCallback?.Invoke(new ErrorInfo
                {
                    ErrorLine = context.ProgressInfo?.ProcessedCount,
                    ErrorMessage = ex.ExpandExceptionMessage(),
                });
            }
            finally
            {
                await FinishRunAsync(dataImporter, importReporter, importRemainingEstimator, writer, context, errors, progressCallback);
            }
        }

        private async Task<IImportReporter> CreateReporterAsync(ImportProfile importProfile)
        {
            var defaultImportReporterType = await _settingsManager.GetValueAsync<string>(ModuleConstants.Settings.General.DefaultImportReporter);
            var importReporterType = !string.IsNullOrEmpty(importProfile.ImportReporterType) ? importProfile.ImportReporterType : defaultImportReporterType;
            var importReporter = _importReporterFactory.Create(importReporterType);

            // The caller's `using` only takes ownership once this method returns.
            try
            {
                importReporter.SetContext(importProfile);
            }
            catch
            {
                importReporter.Dispose();
                throw;
            }

            return importReporter;
        }

        private static bool HasStoredCursor(ImportProfile importProfile) => !string.IsNullOrEmpty(importProfile.RunHistory?.Cursor);

        private async Task ReadAndWritePagesAsync(
            ImportContext context,
            IImportDataReader reader,
            IImportDataWriter writer,
            ImportErrorCollector errors,
            IImportRemainingEstimator importRemainingEstimator,
            Func<ImportProgressInfo, Task> progressCallback,
            CancellationToken token)
        {
            var importProgress = context.ProgressInfo;

            var checkpointTracker = new CursorCheckpointTracker(context.CursorSaveIntervalPages);
            var cursorReader = reader as IResumableImportDataReader;

            do
            {
                token.ThrowIfCancellationRequested();

                await checkpointTracker.TrySaveCursorAsync(context, cursorReader, writer);

                var items = await reader.ReadNextPageAsync(context);

                token.ThrowIfCancellationRequested();

                await writer.WriteAsync(items, context);
                importProgress.ProcessedCount += items.Length;

                importRemainingEstimator.Update(context);
                importRemainingEstimator.Estimate(context);

                await progressCallback(importProgress);

                CursorCheckpointTracker.ClearSaveState(context);

            } while (reader.HasMoreResults && !errors.LimitReached);

            // Only an exhausted source completes the run; a loop the error limit stopped early does not.
            context.IsCompleted = !reader.HasMoreResults;
        }

        private async Task FinishRunAsync(
            IDataImporter dataImporter,
            IImportReporter importReporter,
            IImportRemainingEstimator importRemainingEstimator,
            IImportDataWriter writer,
            ImportContext context,
            ImportErrorCollector errors,
            Func<ImportProgressInfo, Task> progressCallback)
        {
            var importProgress = context.ProgressInfo;

            if (!await FlushSafelyAsync(writer, context))
            {
                context.IsCompleted = false;
            }

            var errorReportResult = await SaveErrorsSafelyAsync(importReporter, errors.GetTopErrors(), context);
            importRemainingEstimator.Stop(context);

            importProgress.Finished = DateTime.UtcNow;
            importProgress.ReportUrl = errorReportResult ?? importProgress.ReportUrl;

            ExceptionDispatchInfo completionFailure = null;
            try
            {
                await dataImporter.OnImportCompletedAsync(context);
            }
            catch (Exception ex)
            {
                // Logged here: the final progress below can throw too, and would replace it.
                LogCompletionHookFailed(ex, context.ImportProfile.Name);
                completionFailure = ExceptionDispatchInfo.Capture(ex);
            }

            importProgress.Description = completionFailure is null
                ? $"Import completed {(importProgress.Errors?.Count > 0 ? "with errors" : "successfully")}"
                : "Import failed";

            // The final progress carries Finished, the report url and the flush and report errors to the run history row.
            await progressCallback(importProgress);

            completionFailure?.Throw();
        }

        // A value stored on the profile (an importer that registers the setting for its own profiles) wins over the
        // module-level setting, which in turn falls back to the descriptor default.
        private async Task<int> GetCursorSettingAsync(ImportProfile importProfile, SettingDescriptor descriptor)
        {
            var hasStoredValue = importProfile.Settings?.Any(x => x.Name.EqualsIgnoreCase(descriptor.Name) && x.Value is not null) == true;

            return hasStoredValue
                ? importProfile.Settings.GetValue<int>(descriptor)
                : await _settingsManager.GetValueAsync<int>(descriptor);
        }

        private async Task<bool> FlushSafelyAsync(IImportDataWriter writer, ImportContext context)
        {
            try
            {
                await writer.FlushAsync(context);

                return true;
            }
            catch (Exception ex)
            {
                context.ErrorCallback?.Invoke(new ErrorInfo
                {
                    ErrorLine = context.ProgressInfo?.ProcessedCount,
                    ErrorMessage = ex.ExpandExceptionMessage(),
                });
                LogFlushFailed(ex, context.ImportProfile.Name);

                return false;
            }
        }

        // A report that cannot be saved must not skip Finished, OnImportCompletedAsync and the final progress:
        // the data is already written. The failure is reported like any other error, and the report url stays unset.
        private async Task<string> SaveErrorsSafelyAsync(IImportReporter importReporter, List<ErrorInfo> errorsToSave, ImportContext context)
        {
            try
            {
                return await importReporter.SaveErrorsAsync(errorsToSave);
            }
            catch (Exception ex)
            {
                context.ErrorCallback?.Invoke(new ErrorInfo
                {
                    ErrorLine = context.ProgressInfo?.ProcessedCount,
                    ErrorMessage = ex.ExpandExceptionMessage(),
                });
                LogSaveErrorsFailed(ex, context.ImportProfile.Name);

                return null;
            }
        }

        /// <summary>
        /// Attempts to restore reader state from the import run history's serialized cursor.
        /// On success: injects the cursor's embedded ProcessedCount into context.ProgressInfo
        /// (via <see cref="IResumableImportDataReader"/> DIM bridge) and returns true.
        /// Without a cursor: returns false and leaves the row untouched.
        /// On a cursor the reader cannot use — a reader that is not resumable, or an invalid or expired cursor: resets
        /// the history row (Cursor/ProcessedCount/Errors/ErrorsCount) and returns false, so the pipeline continues as a
        /// fresh run. The errors seeded from the replaced run are removed
        /// from <paramref name="errors"/>; errors this run already raised (for example while opening the reader) stay on the row.
        /// The reset clears the row, then sends the cleaned progress through <paramref name="progressCallback"/>, then saves the row.
        /// A restore that throws is rethrown and fails the run.
        /// </summary>
        internal async Task<bool> TryRestoreCursorAsync(IImportDataReader reader, ImportContext context, ImportErrorCollector errors, Func<ImportProgressInfo, Task> progressCallback)
        {
            var runHistory = context.ImportProfile.RunHistory;
            if (string.IsNullOrEmpty(runHistory?.Cursor))
            {
                return false;
            }

            if (reader is not IResumableImportDataReader cursorReader)
            {
                LogCursorFromImportRunHistoryNotResumable(runHistory.Id);
                await ResetRunHistoryAsync(runHistory, context, errors, progressCallback);

                return false;
            }

            try
            {
                if (cursorReader.TryRestoreFromSerializedCursor(context, runHistory.Cursor))
                {
                    LogRestoredCursorFromImportRunHistory(runHistory.Id, context.ProgressInfo?.ProcessedCount ?? 0);
                    return true;
                }
            }
            catch (Exception ex)
            {
                // RestoreCursor may have partially advanced the reader before throwing
                // (read header, skipped N rows). Resetting and continuing would make the
                // pipeline skip those rows silently. Surface the error so the run fails
                // explicitly; the user can investigate and restart manually.
                LogFailedToRestoreCursorFromImportRunHistory(ex, runHistory.Id);
                throw;
            }

            LogCursorFromImportRunHistoryInvalid(runHistory.Id);
            await ResetRunHistoryAsync(runHistory, context, errors, progressCallback);

            return false;
        }

        private async Task ResetRunHistoryAsync(ImportRunHistory runHistory, ImportContext context, ImportErrorCollector errors, Func<ImportProgressInfo, Task> progressCallback)
        {
            // A clean start inherits nothing from the run it replaces; errors this run raised at open time stay.
            errors.RemoveSeeded();

            runHistory.Cursor = null;
            runHistory.ProcessedCount = 0;
            runHistory.Errors = context.ProgressInfo?.Errors?.ToList() ?? [];
            runHistory.ErrorsCount = runHistory.Errors.Count;

            // Notified after the row is reset and before it is saved: the notification still holds the list rendered
            // before the reset, and a failing notification, save or start hook finishes the row from the notification.
            await progressCallback(context.ProgressInfo);

            await _importRunHistoryService.SaveChangesAsync([runHistory]);
        }

        [LoggerMessage(LogLevel.Error, "OnImportCompletedAsync failed for import profile '{profileName}'")]
        partial void LogCompletionHookFailed(Exception exception, string profileName);

        [LoggerMessage(LogLevel.Error, "FlushAsync failed for import profile '{profileName}'")]
        partial void LogFlushFailed(Exception exception, string profileName);

        [LoggerMessage(LogLevel.Error, "Saving the error report failed for import profile '{profileName}'")]
        partial void LogSaveErrorsFailed(Exception exception, string profileName);

        [LoggerMessage(LogLevel.Information, "Restored cursor from import run history '{HistoryId}' at {ProcessedCount} processed records")]
        partial void LogRestoredCursorFromImportRunHistory(string historyId, int processedCount);

        [LoggerMessage(LogLevel.Error, "Failed to restore cursor from import run history '{historyId}'")]
        partial void LogFailedToRestoreCursorFromImportRunHistory(Exception exception, string historyId);

        [LoggerMessage(LogLevel.Warning, "Cursor from import run history '{HistoryId}' is expired or invalid, restarting from zero")]
        partial void LogCursorFromImportRunHistoryInvalid(string historyId);

        [LoggerMessage(LogLevel.Warning, "Import run history '{HistoryId}' has a cursor, but the reader is not resumable; starting a fresh run")]
        partial void LogCursorFromImportRunHistoryNotResumable(string historyId);
    }
}
