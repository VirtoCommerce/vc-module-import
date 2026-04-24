using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class DataImportProcessManagerLoopTests
    {
        public sealed record SeqCursor(int PageIdx) : ImportDataCursor;

        private sealed class SeqReader : IImportDataReader<SeqCursor>
        {
            public int PageIdx { get; private set; }
            public int TotalPages { get; init; } = 5;
            public int PageSize { get; init; } = 10;
            public bool HasMoreResults => PageIdx < TotalPages;
            public SeqCursor GetCursor(ImportContext context) => new SeqCursor(PageIdx);
            public void RestoreCursor(ImportContext context, SeqCursor cursor) => PageIdx = cursor.PageIdx;
            public Task<int> GetTotalCountAsync(ImportContext context) => Task.FromResult(TotalPages * PageSize);
            public Task<object[]> ReadNextPageAsync(ImportContext context)
            {
                var page = new object[PageSize];
                PageIdx++;
                return Task.FromResult(page);
            }
            public void Dispose() { }
        }

        private sealed class TraceWriter : IImportDataWriter
        {
            public List<string> Calls { get; } = new();
            public Task WriteAsync(object[] items, ImportContext context)
            {
                Calls.Add($"Write:{items.Length}");
                return Task.CompletedTask;
            }
            public Task FlushAsync(ImportContext context)
            {
                Calls.Add("Flush");
                return Task.CompletedTask;
            }
            public void Dispose() { }
        }

        [Fact]
        public async Task Pipeline_Calls_FlushAsync_At_Throttle_Checkpoints_And_In_Finally()
        {
            var reader = new SeqReader();
            var writer = new TraceWriter();
            var profile = MakeProfile(saveInterval: 2);

            var progressEvents = new List<(string Cursor, bool ShouldSave, int PC)>();
            var manager = TestHelperFactory.CreateManagerWithImporter(reader, writer);

            await manager.ImportAsync(profile, info =>
            {
                progressEvents.Add((info.Cursor, info.ShouldSaveHistory, info.ProcessedCount));
                return Task.CompletedTask;
            }, CancellationToken.None);

            // SaveIntervalPages=2, TotalPages=5:
            //   iter 1 top: PC=0, skip
            //   iter 2 top: PC=10, pagesSinceLast=1, skip
            //   iter 3 top: PC=20, pagesSinceLast=2, Flush + capture
            //   iter 4 top: PC=30, pagesSinceLast=1, skip
            //   iter 5 top: PC=40, pagesSinceLast=2, Flush + capture
            //   finally:    Flush
            // = 3 Flush calls total.
            Assert.Equal(3, writer.Calls.Count(c => c == "Flush"));

            // Events with ShouldSaveHistory=true: 2 (at bottom of iters 3 and 5)
            Assert.Equal(2, progressEvents.Count(e => e.ShouldSave));
        }

        private static ImportProfile MakeProfile(int saveInterval) =>
            new()
            {
                Settings = new List<ObjectSettingEntry>
                {
                    new()
                    {
                        Name = ImportCursorSettings.SaveIntervalPages.Name,
                        Value = saveInterval,
                        ValueType = SettingValueType.PositiveInteger,
                    },
                    new()
                    {
                        Name = ImportCursorSettings.LifetimeDays.Name,
                        Value = 7,
                        ValueType = SettingValueType.PositiveInteger,
                    },
                },
            };
    }
}
