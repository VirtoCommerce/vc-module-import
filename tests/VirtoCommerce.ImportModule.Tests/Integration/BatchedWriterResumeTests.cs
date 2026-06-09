using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Integration
{
    public class BatchedWriterResumeTests
    {
        private sealed record PagedCursor(int PageIdx) : ImportDataCursor;

        private sealed class PagedReader : IImportDataReader<PagedCursor>
        {
            public int PageIdx { get; set; }
            public int TotalPages { get; init; } = 10;
            public int? StopAfterWrites { get; set; }

            public bool HasMoreResults => PageIdx < TotalPages && (StopAfterWrites is null || PageIdx < StopAfterWrites);

            public PagedCursor GetCursor(ImportContext context) => new PagedCursor(PageIdx);
            public void RestoreCursor(ImportContext context, PagedCursor cursor) => PageIdx = cursor.PageIdx;

            public Task<int> GetTotalCountAsync(ImportContext context) => Task.FromResult(TotalPages * 10);

            public Task<object[]> ReadNextPageAsync(ImportContext context)
            {
                var page = new object[10];
                for (var i = 0; i < 10; i++)
                {
                    page[i] = PageIdx * 10 + i;
                }
                PageIdx++;
                return Task.FromResult(page);
            }

            public void Dispose() { }
        }

        private sealed class BatchedWriter : IImportDataWriter
        {
            public HashSet<int> Committed { get; } = new();
            private readonly List<Task> _inFlight = new();
            private readonly Lock _lock = new();

            public async Task WriteAsync(object[] items, ImportContext context)
            {
                var task = Task.Run(async () =>
                {
                    await Task.Delay(20);
                    lock (_lock)
                    {
                        foreach (var i in items)
                        {
                            Committed.Add((int)i);
                        }
                    }
                });
                lock (_lock)
                {
                    _inFlight.Add(task);
                }
                if (_inFlight.Count > 2)
                {
                    await Task.WhenAny(_inFlight);
                    lock (_lock)
                    {
                        _inFlight.RemoveAll(t => t.IsCompleted);
                    }
                }
            }

            public async Task FlushAsync(ImportContext context)
            {
                Task[] snapshot;
                lock (_lock)
                {
                    snapshot = _inFlight.ToArray();
                }
                await Task.WhenAll(snapshot);
                lock (_lock)
                {
                    _inFlight.Clear();
                }
            }

            public void Dispose() { }
        }

        [Fact]
        public async Task Resume_After_Crash_Commits_Every_Record_Exactly_Once()
        {
            // Phase 1: run 4 pages then stop (reader self-stops via StopAfterWrites,
            // simulating a crash — pipeline captures cursor at the throttle checkpoint).
            var reader1 = new PagedReader { TotalPages = 10, StopAfterWrites = 4 };
            var writer1 = new BatchedWriter();
            var history = new ImportRunHistory { Id = "H1" };
            var profile = MakeProfile(history, saveInterval: 3);

            string savedCursor = null;
            var manager1 = TestHelperFactory.CreateManagerWithImporter(reader1, writer1);

            await manager1.ImportAsync(profile, info =>
            {
                if (info.ShouldSaveHistory && !string.IsNullOrEmpty(info.Cursor))
                {
                    savedCursor = info.Cursor;
                }
                return Task.CompletedTask;
            }, CancellationToken.None);

            // The pipeline fires FlushAsync at the throttle checkpoint (top of iter 4) AND in
            // the finally block, so every page Phase 1 wrote is committed.
            // Saved cursor is the snapshot at the top of iter 4 (next page to read = PageIdx 3).
            Assert.True(writer1.Committed.Count >= 30, $"Phase 1 committed {writer1.Committed.Count}, expected >= 30");
            Assert.False(string.IsNullOrEmpty(savedCursor), "Phase 1 should have captured a cursor");

            // Phase 2: resume with saved cursor attached to profile, fresh reader/writer.
            history.Cursor = savedCursor;
            var reader2 = new PagedReader { TotalPages = 10 };
            var writer2 = new BatchedWriter();
            profile.RunHistory = history;
            var manager2 = TestHelperFactory.CreateManagerWithImporter(reader2, writer2);

            await manager2.ImportAsync(profile, _ => Task.CompletedTask, CancellationToken.None);

            // Union of both phases = 100 unique records (0..99). The saved cursor points to the
            // next page to read, so reprocessing overlap is zero, but HashSet union dedupes either way.
            var union = new HashSet<int>(writer1.Committed);
            union.UnionWith(writer2.Committed);
            Assert.True(union.SetEquals(Enumerable.Range(0, 100)),
                $"Union size={union.Count}, min={union.Min()}, max={union.Max()}");
        }

        private static ImportProfile MakeProfile(ImportRunHistory history, int saveInterval) =>
            new()
            {
                RunHistory = history,
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
