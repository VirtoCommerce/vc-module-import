using System;
using System.Collections.Generic;
using System.Linq;
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
    public class DataImportProcessManagerErrorSeedingTests
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

        private sealed class NoopWriter : IImportDataWriter
        {
            public Task WriteAsync(object[] items, ImportContext context) => Task.CompletedTask;
            public Task FlushAsync(ImportContext context) => Task.CompletedTask;
            public void Dispose() { }
        }

        // ErrorLine = 1 renders as "Line 1: <message>" whatever ProcessedCount is at that moment.
        private sealed class ErrorOnFirstWriteWriter(string message) : IImportDataWriter
        {
            private bool _reported;

            public Task WriteAsync(object[] items, ImportContext context)
            {
                if (!_reported)
                {
                    _reported = true;
                    context.ErrorCallback(new ErrorInfo { ErrorLine = 1, ErrorMessage = message });
                }

                return Task.CompletedTask;
            }

            public Task FlushAsync(ImportContext context) => Task.CompletedTask;
            public void Dispose() { }
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

        private static ImportRunHistory ResumedRow(string cursor) =>
            new()
            {
                Id = "H1",
                Cursor = cursor,
                Finished = null,   // TryGetRunHistoryAsync has already cleared it on a replay
                Errors = new List<string> { "Line 5: b", "Line 3: a" },
                ErrorsCount = 2,
            };

        [Fact]
        public async Task Resumed_Run_Starts_With_The_Stored_Errors_In_Stored_Order()   // U1
        {
            var reader = new PagedReader { TotalPages = 3 };
            var profile = MakeProfile(saveInterval: 1000);
            profile.RunHistory = ResumedRow(new PageCursor(1) { ProcessedCount = 1 }.Serialize());
            IList<string> firstErrors = null;
            var manager = TestHelperFactory.CreateManagerWithImporter(reader, new NoopWriter());

            await manager.ImportAsync(profile, x =>
            {
                firstErrors ??= x.Errors.ToList();
                return Task.CompletedTask;
            }, CancellationToken.None);

            Assert.Equal(["Line 5: b", "Line 3: a"], firstErrors);
        }

        [Fact]
        public async Task Invalid_Cursor_Removes_Seeded_Errors_And_Keeps_Open_Time_Errors()   // U4
        {
            var reader = new PagedReader { TotalPages = 2 };
            var profile = MakeProfile(saveInterval: 1000);
            profile.RunHistory = ResumedRow("garbage-not-base64");
            List<string> rowErrorsAtReset = null;
            var crud = new Mock<IImportRunHistoryCrudService>();
            crud.Setup(x => x.SaveChangesAsync(It.IsAny<IList<ImportRunHistory>>()))
                .Callback<IList<ImportRunHistory>>(x => rowErrorsAtReset = x[0].Errors.ToList())
                .Returns(Task.CompletedTask);
            List<ErrorInfo> reported = null;
            var reporter = new Mock<IImportReporter>();
            reporter.Setup(x => x.SaveErrorsAsync(It.IsAny<List<ErrorInfo>>()))
                .Callback<List<ErrorInfo>>(x => reported = x)
                .ReturnsAsync((string)null);
            IList<string> lastErrors = null;
            var manager = TestHelperFactory.CreateManagerWithImporter(reader, new NoopWriter(),
                configureImporter: importer => importer
                    .Setup(x => x.OpenReaderAsync(It.IsAny<ImportContext>()))
                    .Callback<ImportContext>(x => x.ErrorCallback(new ErrorInfo { ErrorLine = 0, ErrorMessage = "open" }))
                    .ReturnsAsync(reader),
                reporter: reporter.Object,
                historyCrud: crud.Object);

            await manager.ImportAsync(profile, x =>
            {
                lastErrors = x.Errors.ToList();
                return Task.CompletedTask;
            }, CancellationToken.None);

            Assert.Equal(["Line 0: open"], rowErrorsAtReset);
            Assert.Equal(["Line 0: open"], lastErrors);
            Assert.Equal(["open"], reported.Select(x => x.ErrorMessage));
        }

        [Fact]
        public async Task Finished_Resumed_Run_Row_Holds_Seeded_And_New_Errors()   // U6
        {
            var reader = new PagedReader { TotalPages = 2 };
            var profile = MakeProfile(saveInterval: 1000);
            var service = TestHelperFactory.CreateTestableRunService(Mock.Of<IImportRunHistoryCrudService>(), out var history, out var notification);
            history.Cursor = new PageCursor(1) { ProcessedCount = 1 }.Serialize();
            history.Errors = new List<string> { "Line 5: b", "Line 3: a" };
            profile.RunHistory = history;
            var writer = new ErrorOnFirstWriteWriter("new");
            var manager = TestHelperFactory.CreateManagerWithImporter(reader, writer);

            await manager.ImportAsync(profile, x => service.InvokeCallbackForTesting(x, notification, history), CancellationToken.None);
            // What RunImportAsync's finally does with the same row instance
            history.Finish(notification);

            Assert.Equal(["Line 1: new", "Line 5: b", "Line 3: a"], history.Errors.ToList());
        }

        [Fact]
        public async Task Error_Report_Of_A_Resumed_Run_Carries_Each_Seeded_Entry_As_Its_Message()   // U18
        {
            var reader = new PagedReader { TotalPages = 3 };
            var profile = MakeProfile(saveInterval: 1000);
            profile.RunHistory = ResumedRow(new PageCursor(1) { ProcessedCount = 1 }.Serialize());
            List<ErrorInfo> reported = null;
            var reporter = new Mock<IImportReporter>();
            reporter.Setup(x => x.SaveErrorsAsync(It.IsAny<List<ErrorInfo>>()))
                .Callback<List<ErrorInfo>>(x => reported = x)
                .ReturnsAsync((string)null);
            var manager = TestHelperFactory.CreateManagerWithImporter(reader, new NoopWriter(), reporter: reporter.Object);

            await manager.ImportAsync(profile, _ => Task.CompletedTask, CancellationToken.None);

            Assert.Equal(["Line 5: b", "Line 3: a"], reported.Select(x => x.ErrorMessage));
        }

        [Fact]
        public async Task Reset_Reaches_The_Notification_Before_A_Failing_Start_Hook()   // U4, start hook throws
        {
            var reader = new PagedReader { TotalPages = 2 };
            var profile = MakeProfile(saveInterval: 1000);
            profile.RunHistory = ResumedRow("garbage-not-base64");
            IList<string> lastErrors = null;
            var manager = TestHelperFactory.CreateManagerWithImporter(reader, new NoopWriter(),
                configureImporter: importer =>
                {
                    importer.Setup(x => x.OpenReaderAsync(It.IsAny<ImportContext>()))
                        .Callback<ImportContext>(x => x.ErrorCallback(new ErrorInfo { ErrorLine = 0, ErrorMessage = "open" }))
                        .ReturnsAsync(reader);
                    importer.Setup(x => x.OnImportStartedAsync(It.IsAny<ImportContext>())).ThrowsAsync(new InvalidOperationException("refused"));
                },
                historyCrud: Mock.Of<IImportRunHistoryCrudService>());

            await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ImportAsync(profile, x =>
            {
                lastErrors = x.Errors.ToList();
                return Task.CompletedTask;
            }, CancellationToken.None));

            Assert.Equal(["Line 0: open"], lastErrors);
        }
    }
}
