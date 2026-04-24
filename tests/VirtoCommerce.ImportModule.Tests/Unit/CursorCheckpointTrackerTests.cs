using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Services;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class CursorCheckpointTrackerTests
    {
        private sealed class FakeCursorReader : IResumableImportDataReader
        {
            public bool HasStableCursor { get; set; } = true;
            public string CursorToReturn { get; set; } = "cursor-v1";
            public int GetSerializedCursorCalls { get; private set; }

            public string GetSerializedCursor(ImportContext context)
            {
                GetSerializedCursorCalls++;
                return CursorToReturn;
            }

            public bool TryRestoreFromSerializedCursor(ImportContext context, string serializedCursor) => true;
        }

        private sealed class TraceWriter : IImportDataWriter
        {
            public int FlushCalls { get; private set; }

            public Task WriteAsync(object[] items, ImportContext context) => Task.CompletedTask;

            public Task FlushAsync(ImportContext context)
            {
                FlushCalls++;
                return Task.CompletedTask;
            }

            public void Dispose() { }
        }

        private static (ImportContext ctx, ImportProgressInfo progress) MakeContext(int processedCount = 100)
        {
            var progress = new ImportProgressInfo { ProcessedCount = processedCount };
            var ctx = new ImportContext(new ImportProfile()) { ProgressInfo = progress };
            return (ctx, progress);
        }

        [Fact]
        public async Task No_save_before_interval()
        {
            var tracker = new CursorCheckpointTracker(saveIntervalPages: 3);
            var reader = new FakeCursorReader();
            var writer = new TraceWriter();
            var (ctx, progress) = MakeContext();

            await tracker.TrySaveCursorAsync(ctx, reader, writer);
            await tracker.TrySaveCursorAsync(ctx, reader, writer);

            Assert.Equal(0, writer.FlushCalls);
            Assert.Equal(0, reader.GetSerializedCursorCalls);
            Assert.Null(progress.Cursor);
            Assert.False(progress.ShouldSaveHistory);
        }

        [Fact]
        public async Task Saves_when_interval_reached_and_cursor_stable()
        {
            var tracker = new CursorCheckpointTracker(saveIntervalPages: 2);
            var reader = new FakeCursorReader();
            var writer = new TraceWriter();
            var (ctx, progress) = MakeContext();

            await tracker.TrySaveCursorAsync(ctx, reader, writer);
            await tracker.TrySaveCursorAsync(ctx, reader, writer);

            Assert.Equal(1, writer.FlushCalls);
            Assert.Equal(1, reader.GetSerializedCursorCalls);
            Assert.Equal("cursor-v1", progress.Cursor);
            Assert.True(progress.ShouldSaveHistory);
        }

        [Fact]
        public async Task Counter_resets_after_save_and_saves_again_after_next_interval()
        {
            var tracker = new CursorCheckpointTracker(saveIntervalPages: 2);
            var reader = new FakeCursorReader();
            var writer = new TraceWriter();
            var (ctx, progress) = MakeContext();

            await tracker.TrySaveCursorAsync(ctx, reader, writer);
            await tracker.TrySaveCursorAsync(ctx, reader, writer); // save #1
            progress.Cursor = null;
            progress.ShouldSaveHistory = false;

            await tracker.TrySaveCursorAsync(ctx, reader, writer);
            await tracker.TrySaveCursorAsync(ctx, reader, writer); // save #2

            Assert.Equal(2, writer.FlushCalls);
            Assert.Equal(2, reader.GetSerializedCursorCalls);
        }

        [Fact]
        public async Task Save_deferred_while_cursor_unstable_fires_immediately_once_stable()
        {
            var tracker = new CursorCheckpointTracker(saveIntervalPages: 2);
            var reader = new FakeCursorReader { HasStableCursor = true };
            var writer = new TraceWriter();
            var (ctx, progress) = MakeContext();

            await tracker.TrySaveCursorAsync(ctx, reader, writer); // counter=1

            // Threshold reached but reader says mid-batch.
            reader.HasStableCursor = false;
            await tracker.TrySaveCursorAsync(ctx, reader, writer); // counter=2, deferred
            await tracker.TrySaveCursorAsync(ctx, reader, writer); // counter=3, still deferred

            Assert.Equal(0, writer.FlushCalls);

            // Reader becomes stable — next call should save immediately (no extra wait).
            reader.HasStableCursor = true;
            await tracker.TrySaveCursorAsync(ctx, reader, writer); // counter=4, stable → save

            Assert.Equal(1, writer.FlushCalls);
            Assert.Equal(1, reader.GetSerializedCursorCalls);
            Assert.Equal("cursor-v1", progress.Cursor);
        }

        [Fact]
        public async Task Ignores_null_reader()
        {
            var tracker = new CursorCheckpointTracker(saveIntervalPages: 1);
            var writer = new TraceWriter();
            var (ctx, _) = MakeContext();

            await tracker.TrySaveCursorAsync(ctx, cursorReader: null, writer);
            await tracker.TrySaveCursorAsync(ctx, cursorReader: null, writer);

            Assert.Equal(0, writer.FlushCalls);
        }

        [Fact]
        public async Task Ignores_when_ProcessedCount_is_zero()
        {
            var tracker = new CursorCheckpointTracker(saveIntervalPages: 1);
            var reader = new FakeCursorReader();
            var writer = new TraceWriter();
            var (ctx, _) = MakeContext(processedCount: 0);

            await tracker.TrySaveCursorAsync(ctx, reader, writer);

            Assert.Equal(0, writer.FlushCalls);
            Assert.Equal(0, reader.GetSerializedCursorCalls);
        }

        [Fact]
        public void ClearSaveState_clears_Cursor_and_ShouldSaveHistory()
        {
            var (ctx, progress) = MakeContext();
            progress.Cursor = "previous-cursor";
            progress.ShouldSaveHistory = true;

            CursorCheckpointTracker.ClearSaveState(ctx);

            Assert.Null(progress.Cursor);
            Assert.False(progress.ShouldSaveHistory);
        }

        [Fact]
        public void ClearSaveState_tolerates_null_ProgressInfo()
        {
            var ctx = new ImportContext(new ImportProfile()); // ProgressInfo left null

            CursorCheckpointTracker.ClearSaveState(ctx); // should not throw
        }
    }
}
