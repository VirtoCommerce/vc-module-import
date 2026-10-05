using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Models;

namespace VirtoCommerce.ImportModule.Data.Services
{
    /// <summary>
    /// Owns the per-run cursor-save throttle. Counts pipeline pages between saves and, on
    /// reaching the configured interval, flushes the writer (durability barrier) and captures
    /// a serialized cursor into <see cref="ImportProgressInfo.Cursor"/> for the callback to persist.
    /// A save is deferred (counter preserved) while the reader reports
    /// <see cref="IResumableImportDataReader.HasStableCursor"/> false — e.g. mid-slice of a
    /// pre-fetched batch — and happens immediately on the next call once the reader becomes stable.
    /// </summary>
    public partial class CursorCheckpointTracker
    {
        private readonly int _saveIntervalPages;
        private readonly ILogger _logger;
        private int _pagesSinceLastSave;

        public CursorCheckpointTracker(int saveIntervalPages, ILogger logger)
        {
            _saveIntervalPages = saveIntervalPages;
            _logger = logger;
        }

        public async Task TrySaveCursorAsync(ImportContext context, IResumableImportDataReader cursorReader, IImportDataWriter writer)
        {
            var progress = context.ProgressInfo;
            if (cursorReader is null || progress is null || progress.ProcessedCount <= 0)
            {
                return;
            }

            _pagesSinceLastSave++;
            if (_pagesSinceLastSave < _saveIntervalPages || !cursorReader.HasStableCursor)
            {
                return;
            }

            await writer.FlushAsync(context);

            var serializedCursor = cursorReader.GetSerializedCursor(context);

            // Never truncate: a truncated cursor would silently resume at a wrong position.
            if (serializedCursor?.Length > ImportRunHistoryEntity.CursorMaxLength)
            {
                LogCursorTooLong(context.ImportProfile?.Name, serializedCursor.Length, ImportRunHistoryEntity.CursorMaxLength);
                serializedCursor = null;
            }

            progress.Cursor = serializedCursor;
            progress.ShouldSaveHistory = !string.IsNullOrEmpty(progress.Cursor);

            _pagesSinceLastSave = 0;
        }

        /// <summary>
        /// Clears <see cref="ImportProgressInfo.Cursor"/> and <see cref="ImportProgressInfo.ShouldSaveHistory"/>
        /// after the progress callback has persisted them, so the next iteration starts clean.
        /// </summary>
        public static void ClearSaveState(ImportContext context)
        {
            context.ProgressInfo?.Cursor = null;
            context.ProgressInfo?.ShouldSaveHistory = false;
        }

        [LoggerMessage(LogLevel.Warning, "Skipped the cursor checkpoint of import profile '{ProfileName}': the serialized cursor is {Length} characters, longer than the {MaxLength} the run history can store")]
        partial void LogCursorTooLong(string profileName, int length, int maxLength);
    }
}
