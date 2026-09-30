using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class DataImportProcessManagerCompletionTests
    {
        private sealed record PageCursor(int PageIdx) : ImportDataCursor;

        private sealed class PagedReader : IImportDataReader<PageCursor>
        {
            public int PageIdx { get; private set; }
            public int TotalPages { get; init; } = 3;
            public bool HasMoreResults => PageIdx < TotalPages;
            public PageCursor GetCursor(ImportContext context) => new(PageIdx);
            public void RestoreCursor(ImportContext context, PageCursor cursor) => PageIdx = cursor.PageIdx;
            public Task<int> GetTotalCountAsync(ImportContext context) => Task.FromResult(TotalPages);
            public Task<object[]> ReadNextPageAsync(ImportContext context)
            {
                PageIdx++;
                return Task.FromResult(new object[1]);
            }
            public void Dispose() { }
        }

        private sealed class ScriptedWriter : IImportDataWriter
        {
            private int _writes;
            public int? ThrowOnWrite { get; init; }
            public ISet<int> ErrorOnWrites { get; init; } = new HashSet<int>();
            public bool ThrowOnFlush { get; init; }
            public bool ErrorOnFlush { get; init; }

            public Task WriteAsync(object[] items, ImportContext context)
            {
                _writes++;
                if (_writes == ThrowOnWrite)
                {
                    throw new InvalidOperationException("write failed");
                }
                if (ErrorOnWrites.Contains(_writes))
                {
                    context.ErrorCallback(new ErrorInfo { ErrorLine = _writes, ErrorMessage = $"rejected {_writes}" });
                }
                return Task.CompletedTask;
            }

            public Task FlushAsync(ImportContext context)
            {
                if (ThrowOnFlush)
                {
                    throw new InvalidOperationException("flush failed");
                }
                if (ErrorOnFlush)
                {
                    context.ErrorCallback(new ErrorInfo { ErrorLine = 0, ErrorMessage = "rejected at flush" });
                }
                return Task.CompletedTask;
            }

            public void Dispose() { }
        }

        private static ImportProfile MakeProfile() =>
            new()
            {
                Settings = new List<ObjectSettingEntry>
                {
                    // Large interval: the only flush the tests reason about is the one in finally
                    new() { Name = ImportCursorSettings.SaveIntervalPages.Name, Value = 1000, ValueType = SettingValueType.PositiveInteger, },
                    new() { Name = ImportCursorSettings.LifetimeDays.Name, Value = 7, ValueType = SettingValueType.PositiveInteger, },
                },
            };

        private static async Task<bool?> RunAndCaptureCompleted(PagedReader reader, ScriptedWriter writer, int? threshold = null, IImportReporter reporter = null)
        {
            bool? completed = null;
            var manager = TestHelperFactory.CreateManagerWithImporter(reader, writer,
                configureImporter: importer => importer
                    .Setup(x => x.OnImportCompletedAsync(It.IsAny<ImportContext>()))
                    .Callback<ImportContext>(x => completed = x.IsCompleted)
                    .Returns(Task.CompletedTask),
                reporter: reporter,
                maxErrorsCountThreshold: threshold);

            await manager.ImportAsync(MakeProfile(), _ => Task.CompletedTask, CancellationToken.None);

            return completed;
        }

        [Fact]
        public async Task Exhausted_Source_And_Successful_Flush_Marks_Run_Completed()
        {
            var completed = await RunAndCaptureCompleted(new PagedReader(), new ScriptedWriter());

            Assert.True(completed);
        }

        [Fact]
        public async Task Throwing_Write_Of_Final_Page_Leaves_Run_Not_Completed()
        {
            // The reader advances in ReadNextPageAsync, so HasMoreResults is already false when the final write throws
            var completed = await RunAndCaptureCompleted(new PagedReader { TotalPages = 3 }, new ScriptedWriter { ThrowOnWrite = 3 });

            Assert.False(completed);
        }

        [Fact]
        public async Task Error_Limit_Before_End_Of_Source_Leaves_Run_Not_Completed()
        {
            var completed = await RunAndCaptureCompleted(
                new PagedReader { TotalPages = 5 },
                new ScriptedWriter { ErrorOnWrites = new HashSet<int> { 1, 2 } },
                threshold: 2);

            Assert.False(completed);
        }

        [Fact]
        public async Task Failed_Final_Flush_Leaves_Run_Not_Completed()
        {
            var completed = await RunAndCaptureCompleted(new PagedReader(), new ScriptedWriter { ThrowOnFlush = true });

            Assert.False(completed);
        }

        [Fact]
        public async Task Error_Limit_Reached_On_Final_Page_Still_Marks_Run_Completed()
        {
            var completed = await RunAndCaptureCompleted(
                new PagedReader { TotalPages = 3 },
                new ScriptedWriter { ErrorOnWrites = new HashSet<int> { 2, 3 } },
                threshold: 2);

            Assert.True(completed);
        }

        [Fact]
        public async Task Error_Limit_Reached_During_Final_Flush_Still_Marks_Run_Completed()
        {
            var completed = await RunAndCaptureCompleted(
                new PagedReader { TotalPages = 3 },
                new ScriptedWriter { ErrorOnWrites = new HashSet<int> { 3 }, ErrorOnFlush = true },
                threshold: 2);

            Assert.True(completed);
        }

        private static IImportReporter ThrowingReporter()
        {
            var reporter = new Mock<IImportReporter>();
            reporter.Setup(x => x.SaveErrorsAsync(It.IsAny<List<ErrorInfo>>())).ThrowsAsync(new InvalidOperationException("report failed"));
            return reporter.Object;
        }

        [Fact]
        public async Task Throwing_Reporter_Still_Completes_The_Run()
        {
            var completed = await RunAndCaptureCompleted(new PagedReader(), new ScriptedWriter(), reporter: ThrowingReporter());

            // Not null: the completion hook ran; true: a failed report does not un-complete the data
            Assert.True(completed);
        }

        [Fact]
        public async Task Throwing_Reporter_Is_Reported_And_Final_Progress_Is_Sent()
        {
            var progress = new List<ImportProgressInfo>();
            var manager = TestHelperFactory.CreateManagerWithImporter(new PagedReader(), new ScriptedWriter(), reporter: ThrowingReporter());

            await manager.ImportAsync(MakeProfile(), x => { progress.Add(x); return Task.CompletedTask; }, CancellationToken.None);

            var last = progress[^1];
            Assert.NotNull(last.Finished);
            Assert.Contains(last.Errors, x => x.Contains("report failed"));
            Assert.Null(last.ReportUrl);
        }
    }
}
