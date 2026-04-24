using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class DataImportProcessManagerRestoreTests
    {
        private sealed record TestCursor(int Skip) : ImportDataCursor;

        private sealed class FakeResumableReader : IImportDataReader<TestCursor>
        {
            public TestCursor Restored { get; private set; }
            public TestCursor GetCursor(ImportContext context) => new(0);
            public void RestoreCursor(ImportContext context, TestCursor cursor) => Restored = cursor;
            public bool HasMoreResults => true;
            public Task<int> GetTotalCountAsync(ImportContext context) => Task.FromResult(0);
            public Task<object[]> ReadNextPageAsync(ImportContext context) => Task.FromResult<object[]>([]);
            public void Dispose() { }
        }

        private sealed class ThrowingReader : IImportDataReader<TestCursor>
        {
            public TestCursor GetCursor(ImportContext context) => new(0);
            public void RestoreCursor(ImportContext context, TestCursor cursor) => throw new InvalidOperationException("boom");
            public bool HasMoreResults => true;
            public Task<int> GetTotalCountAsync(ImportContext context) => Task.FromResult(0);
            public Task<object[]> ReadNextPageAsync(ImportContext context) => Task.FromResult<object[]>([]);
            public void Dispose() { }
        }

        [Fact]
        public async Task Happy_Path_Returns_True_Restores_Reader_And_PC()
        {
            var cursor = new TestCursor(100) { ProcessedCount = 500 };
            var history = new ImportRunHistory { Id = "H1", Cursor = cursor.Serialize(), ProcessedCount = 500 };
            var profile = MakeProfile(history, lifetimeDays: 7);
            var reader = new FakeResumableReader();
            var context = new ImportContext(profile) { ProgressInfo = new ImportProgressInfo() };
            var crud = new Mock<IImportRunHistoryCrudService>();
            var manager = TestHelperFactory.CreateManager(historyCrud: crud.Object);

            var result = await manager.TryRestoreCursorAsync(reader, context);

            Assert.True(result);
            Assert.Equal(100, reader.Restored.Skip);
            Assert.Equal(500, context.ProgressInfo.ProcessedCount);
            crud.Verify(x => x.SaveChangesAsync(It.IsAny<IList<ImportRunHistory>>()), Times.Never);
        }

        [Fact]
        public async Task Invalid_Cursor_Resets_History_Row_And_Returns_False()
        {
            var history = new ImportRunHistory { Id = "H1", Cursor = "garbage-not-base64", ProcessedCount = 500, ErrorsCount = 3 };
            var profile = MakeProfile(history, lifetimeDays: 7);
            var reader = new FakeResumableReader();
            var context = new ImportContext(profile) { ProgressInfo = new ImportProgressInfo() };
            var crud = new Mock<IImportRunHistoryCrudService>();
            var manager = TestHelperFactory.CreateManager(historyCrud: crud.Object);

            var result = await manager.TryRestoreCursorAsync(reader, context);

            Assert.False(result);
            Assert.Null(history.Cursor);
            Assert.Equal(0, history.ProcessedCount);
            Assert.Equal(0, history.ErrorsCount);
            crud.Verify(x => x.SaveChangesAsync(It.Is<IList<ImportRunHistory>>(a => a.Count == 1 && a[0] == history)), Times.Once);
        }

        [Fact]
        public async Task No_Cursor_In_History_Returns_False_No_Mutation()
        {
            var history = new ImportRunHistory { Id = "H1", Cursor = null, ProcessedCount = 0 };
            var profile = MakeProfile(history, lifetimeDays: 7);
            var reader = new FakeResumableReader();
            var context = new ImportContext(profile) { ProgressInfo = new ImportProgressInfo() };
            var crud = new Mock<IImportRunHistoryCrudService>();
            var manager = TestHelperFactory.CreateManager(historyCrud: crud.Object);

            var result = await manager.TryRestoreCursorAsync(reader, context);

            Assert.False(result);
            crud.Verify(x => x.SaveChangesAsync(It.IsAny<IList<ImportRunHistory>>()), Times.Never);
        }

        [Fact]
        public async Task Reader_Restore_Throws_Propagates_So_Pipeline_Aborts()
        {
            // RestoreCursor may have partially advanced the reader before throwing; silently
            // resetting the history row and continuing would make ReadNextPageAsync skip the
            // already-consumed rows. The exception must propagate so the pipeline's outer
            // catch surfaces the failure via the ErrorCallback.
            var cursor = new TestCursor(100) { ProcessedCount = 500 };
            var history = new ImportRunHistory { Id = "H1", Cursor = cursor.Serialize(), ProcessedCount = 500 };
            var profile = MakeProfile(history, lifetimeDays: 7);
            var reader = new ThrowingReader();
            var context = new ImportContext(profile) { ProgressInfo = new ImportProgressInfo() };
            var crud = new Mock<IImportRunHistoryCrudService>();
            var manager = TestHelperFactory.CreateManager(historyCrud: crud.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => manager.TryRestoreCursorAsync(reader, context));
            Assert.Equal(cursor.Serialize(), history.Cursor);
            crud.Verify(x => x.SaveChangesAsync(It.IsAny<IList<ImportRunHistory>>()), Times.Never);
        }

        private static ImportProfile MakeProfile(ImportRunHistory history, int lifetimeDays) =>
            new()
            {
                RunHistory = history,
                Settings = new List<ObjectSettingEntry>
                {
                    new()
                    {
                        Name = ImportCursorSettings.LifetimeDays.Name,
                        Value = lifetimeDays,
                        ValueType = SettingValueType.PositiveInteger,
                    },
                },
            };
    }
}
