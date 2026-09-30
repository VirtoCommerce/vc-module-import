using System;
using System.Collections.Generic;
using System.Linq;
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
            var defaultImportReporterType = await _settingsManager.GetValueAsync<string>(ModuleConstants.Settings.General.DefaultImportReporter);
            var importReporterType = !string.IsNullOrEmpty(importProfile.ImportReporterType) ? importProfile.ImportReporterType : defaultImportReporterType;
            using var importReporter = _importReporterFactory.Create(importReporterType);
            importReporter.SetContext(importProfile);

            // Import progress
            var importProgress = new ImportProgressInfo
            {
                Description = "Import has been started",
            };

            var errors = new ImportErrorCollector(maxErrorsCountThreshold, importProgress, _logger);

            // A resumed run keeps the errors its interrupted part reported. IsResumable() cannot key this:
            // TryGetRunHistoryAsync has already cleared Finished on the row it attaches. Seeding must precede the
            // first progressCallback, which aliases the row's Errors to ProgressInfo.Errors.
            if (!string.IsNullOrEmpty(importProfile.RunHistory?.Cursor))
            {
                errors.Seed(importProfile.RunHistory.Errors);
            }

            // Import context — via AbstractTypeFactory so downstream can OverrideType with a derived context
            // and attach extra state in OnImportStartedAsync.
            var context = AbstractTypeFactory<ImportContext>.TryCreateInstance<ImportContext>(null, importProfile);
            context.ProgressInfo = importProgress;
            context.ErrorCallback = errors.Handle;

            // Resolved before the restore: the cursor is validated against it.
            context.CursorLifetime = TimeSpan.FromDays(await GetCursorSettingAsync(importProfile, ImportCursorSettings.LifetimeDays));

            importRemainingEstimator.Start(context);
            await progressCallback(importProgress);

            // Reading & writing
            using var reader = await dataImporter.OpenReaderAsync(context);
            using var writer = await dataImporter.OpenWriterAsync(context);

            // Attempt to restore the cursor from the run history.
            // A cursor the reader cannot use resets the history row so the run starts fresh; a throwing restore fails the run.
            var hadCursor = !string.IsNullOrEmpty(importProfile.RunHistory?.Cursor);
            context.IsResume = await TryRestoreCursorAsync(reader, context, errors);
            if (hadCursor && !context.IsResume)
            {
                // The notification still references the error list rendered before the reset; the importer's start hook
                // can throw next, and RunImportAsync would then finish the row from that stale list.
                await progressCallback(importProgress);
            }

            await dataImporter.OnImportStartedAsync(context);

            // Calculate total count
            importProgress.Description = "Evaluating total records counts";
            await progressCallback(importProgress);
            importProgress.TotalCount = await reader.GetTotalCountAsync(context);

            token.ThrowIfCancellationRequested();

            // Start import
            importProgress.Description = "Import in progress";
            await progressCallback(importProgress);

            var saveIntervalPages = await GetCursorSettingAsync(importProfile, ImportCursorSettings.SaveIntervalPages);
            var checkpointTracker = new CursorCheckpointTracker(saveIntervalPages);
            var cursorReader = reader as IResumableImportDataReader;

            try
            {
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
                if (!await FlushSafelyAsync(writer, context))
                {
                    context.IsCompleted = false;
                }

                var errorReportResult = await SaveErrorsSafelyAsync(importReporter, errors.GetTopErrors(), context);
                importRemainingEstimator.Stop(context);

                importProgress.Description = $"Import completed {(importProgress.Errors?.Count > 0 ? "with errors" : "successfully")}";
                importProgress.Finished = DateTime.UtcNow;
                importProgress.ReportUrl = errorReportResult ?? importProgress.ReportUrl;

                await dataImporter.OnImportCompletedAsync(context);
                await progressCallback(importProgress);
            }
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
        /// On a cursor the reader cannot use — a reader that is not resumable, or an invalid or expired cursor: resets the history row (Cursor/ProcessedCount/Errors/ErrorsCount) and
        /// returns false, so the pipeline continues as a fresh run. The errors seeded from the replaced run are removed
        /// from <paramref name="errors"/>; errors this run already raised (for example while opening the reader) stay on the row.
        /// A restore that throws is rethrown and fails the run.
        /// </summary>
        internal async Task<bool> TryRestoreCursorAsync(IImportDataReader reader, ImportContext context, ImportErrorCollector errors)
        {
            var runHistory = context.ImportProfile.RunHistory;
            if (string.IsNullOrEmpty(runHistory?.Cursor))
            {
                return false;
            }

            if (reader is not IResumableImportDataReader cursorReader)
            {
                LogCursorFromImportRunHistoryNotResumable(runHistory.Id);
                await ResetRunHistoryAsync(runHistory, context, errors);

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
            await ResetRunHistoryAsync(runHistory, context, errors);

            return false;
        }

        private async Task ResetRunHistoryAsync(ImportRunHistory runHistory, ImportContext context, ImportErrorCollector errors)
        {
            // A clean start inherits nothing from the run it replaces; errors this run raised at open time stay.
            errors.RemoveSeeded();

            runHistory.Cursor = null;
            runHistory.ProcessedCount = 0;
            runHistory.Errors = context.ProgressInfo?.Errors?.ToList() ?? [];
            runHistory.ErrorsCount = runHistory.Errors.Count;
            await _importRunHistoryService.SaveChangesAsync([runHistory]);
        }

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

        [LoggerMessage(LogLevel.Warning, "Import run history {HistoryId} has a cursor, but the reader is not resumable; starting a fresh run")]
        partial void LogCursorFromImportRunHistoryNotResumable(string historyId);
    }
}
