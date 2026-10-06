using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Models.Search;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Security;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportRunServiceRunTests
    {
        [Fact]
        public async Task Failure_Before_The_First_Progress_Keeps_The_Stored_Errors_Of_A_Resumed_Run()
        {
            var row = new ImportRunHistory
            {
                Id = "H1",
                Cursor = "dummy-cursor",
                Errors = new List<string> { "Line 5: b", "Line 3: a" },
                ErrorsCount = 2,
            };
            var profile = new ImportProfile { RunHistory = row };
            var fixture = new Fixture();
            fixture.ImportThrows(new InvalidOperationException("opening failed"));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.Service.RunImportAsync(profile, new ImportPushNotification("tester") { JobId = "job-1" }, CancellationToken.None));

            // The row saved in finally is the one the failed run leaves behind
            var savedErrors = fixture.SavedErrors[^1];
            Assert.Contains("Line 5: b", savedErrors);
            Assert.Contains("Line 3: a", savedErrors);
            Assert.Contains(savedErrors, x => x.Contains("opening failed"));
        }

        [Fact]
        public async Task Failure_Before_The_First_Progress_Keeps_The_Counts_Of_A_Resumed_Run()
        {
            var row = new ImportRunHistory
            {
                Id = "H1",
                Cursor = "dummy-cursor",
                ProcessedCount = 50,
                TotalCount = 100,
            };
            var fixture = new Fixture();
            fixture.ImportThrows(new InvalidOperationException("opening failed"));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.Service.RunImportAsync(new ImportProfile { RunHistory = row }, new ImportPushNotification("tester"), CancellationToken.None));

            // The row saved in finally is the one the failed run leaves behind
            Assert.Equal((50, 100), fixture.SavedCounts[^1]);
        }

        [Fact]
        public async Task Failure_Records_The_Exception_Message_Not_Its_Type_Name_Or_Stack_Trace()
        {
            var fixture = new Fixture();
            var failure = new InvalidOperationException("import failed", new ArgumentException("inner detail"));
            fixture.ImportThrows(failure);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.Service.RunImportAsync(new ImportProfile(), new ImportPushNotification("tester"), CancellationToken.None));

            var recorded = Assert.Single(fixture.SavedErrors[^1]);
            Assert.Contains("import failed", recorded);
            Assert.Contains("inner detail", recorded);
            Assert.DoesNotContain(typeof(InvalidOperationException).FullName, recorded);
            Assert.DoesNotContain(recorded.Split('\n'), x => x.StartsWith("   at "));
        }

        [Fact]
        public async Task Seeded_Notification_Drops_The_Limit_Reached_Message()
        {
            var row = new ImportRunHistory
            {
                Id = "H1",
                Cursor = "dummy-cursor",
                Errors = new List<string> { "Line 5: b", ImportErrorCollector.LimitReachedMessage, "Line 3: a" },
            };
            var fixture = new Fixture();
            fixture.ImportThrows(new InvalidOperationException("opening failed"));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.Service.RunImportAsync(new ImportProfile { RunHistory = row }, new ImportPushNotification("tester"), CancellationToken.None));

            var savedErrors = fixture.SavedErrors[^1];
            Assert.Contains("Line 5: b", savedErrors);
            Assert.Contains("Line 3: a", savedErrors);
            Assert.Contains(savedErrors, x => x.Contains("opening failed"));
            Assert.DoesNotContain(ImportErrorCollector.LimitReachedMessage, savedErrors);
        }

        [Fact]
        public async Task Row_Without_A_Cursor_Does_Not_Seed_The_Notification()
        {
            var row = new ImportRunHistory { Id = "H1", Errors = new List<string> { "Line 5: b" } };
            var fixture = new Fixture();
            fixture.ImportThrows(new InvalidOperationException("opening failed"));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.Service.RunImportAsync(new ImportProfile { RunHistory = row }, new ImportPushNotification("tester"), CancellationToken.None));

            var savedErrors = fixture.SavedErrors[^1];
            Assert.Single(savedErrors);
            Assert.Contains("opening failed", savedErrors[0]);
        }

        [Fact]
        public async Task Row_Is_Saved_Before_The_Finished_Notification_Is_Sent()
        {
            var fixture = new Fixture();

            await fixture.Service.RunImportAsync(new ImportProfile(), new ImportPushNotification("tester"), CancellationToken.None);

            Assert.Contains("send", fixture.Calls);
            Assert.True(fixture.Calls.LastIndexOf("save") < fixture.Calls.LastIndexOf("send"), string.Join(", ", fixture.Calls));
        }

        [Fact]
        public async Task Failing_Finished_Notification_Still_Finishes_The_Row()
        {
            var fixture = new Fixture();
            fixture.SendThrows(new TimeoutException("send failed"));

            await fixture.Service.RunImportAsync(new ImportProfile(), new ImportPushNotification("tester"), CancellationToken.None);

            Assert.NotNull(fixture.SavedFinished[^1]);
            // One failed send is logged once; the "N more" line only appears for more than one failure
            Assert.Single(fixture.Logger.Entries, x => x.Level == LogLevel.Error);
        }

        [Fact]
        public async Task Suppressed_Push_Failures_Are_Logged_When_The_Closing_Save_Throws()
        {
            var fixture = new Fixture();
            var failure = new TimeoutException("send failed");
            fixture.SendThrows(failure);
            fixture.ImportReports(new ImportProgressInfo(), new ImportProgressInfo());
            fixture.SaveThrowsAfter(1, new InvalidOperationException("save failed"));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.Service.RunImportAsync(new ImportProfile(), new ImportPushNotification("tester"), CancellationToken.None));

            // 2 progress + 1 closing = 3 failed sends: the first is logged, the other 2 are counted
            Assert.Equal("save failed", exception.Message);
            var errors = fixture.Logger.Entries.Where(x => x.Level == LogLevel.Error).ToList();
            Assert.Equal(2, errors.Count);
            Assert.Same(failure, errors[0].Exception);
            Assert.Contains("2 more push notifications", errors[1].Message);
        }

        [Fact]
        public async Task Failing_Finished_Notification_Keeps_The_Run_Exception()
        {
            var fixture = new Fixture();
            fixture.ImportThrows(new InvalidOperationException("import failed"));
            fixture.SendThrows(new TimeoutException("send failed"));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.Service.RunImportAsync(new ImportProfile(), new ImportPushNotification("tester"), CancellationToken.None));

            Assert.Equal("import failed", exception.Message);
        }

        [Fact]
        public async Task Failing_Completion_Email_Does_Not_Fail_The_Run()
        {
            var fixture = new Fixture();
            var mailFailure = new TimeoutException("mail down");
            fixture.CompletionEmailFails(mailFailure);

            await fixture.Service.RunImportAsync(new ImportProfile(), new ImportPushNotification("tester"), CancellationToken.None);

            Assert.NotNull(fixture.SavedFinished[^1]);
            var error = Assert.Single(fixture.Logger.Entries, x => x.Level == LogLevel.Error);
            Assert.Same(mailFailure, error.Exception);
        }

        [Fact]
        public async Task Failing_Completion_Email_Keeps_The_Run_Exception()
        {
            var fixture = new Fixture();
            fixture.ImportThrows(new InvalidOperationException("import failed"));
            var mailFailure = new TimeoutException("mail down");
            fixture.CompletionEmailFails(mailFailure);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.Service.RunImportAsync(new ImportProfile(), new ImportPushNotification("tester"), CancellationToken.None));

            Assert.Equal("import failed", exception.Message);
            // The e-mail step ran and its failure was logged, not rethrown
            Assert.Contains(fixture.Logger.Entries, x => x.Exception == mailFailure);
        }

        [Fact]
        public async Task Failing_Closing_Save_Sends_The_Push_But_Not_The_Completion_Email()
        {
            var fixture = new Fixture();
            var mailFailure = new TimeoutException("mail down");
            // Attempting the e-mail would log mailFailure
            fixture.CompletionEmailFails(mailFailure);
            fixture.SaveThrowsAfter(1, new InvalidOperationException("save failed"));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.Service.RunImportAsync(new ImportProfile(), new ImportPushNotification("tester"), CancellationToken.None));

            Assert.Equal("save failed", exception.Message);
            Assert.Equal("send", fixture.Calls[^1]);
            Assert.DoesNotContain(fixture.Logger.Entries, x => x.Exception == mailFailure);
        }

        [Fact]
        public async Task Failing_Progress_Notification_Still_Updates_And_Checkpoints_The_Row()
        {
            var row = new ImportRunHistory { Id = "H1" };
            var fixture = new Fixture();
            fixture.SendThrows(new TimeoutException("send failed"));
            fixture.ImportReports(new ImportProgressInfo { ProcessedCount = 42, TotalCount = 100, Cursor = "cursor-payload", ShouldSaveHistory = true });

            await fixture.Service.RunImportAsync(new ImportProfile { RunHistory = row }, new ImportPushNotification("tester"), CancellationToken.None);

            // Saves: the start of the run, the checkpoint, and the finished row
            Assert.Equal(3, fixture.Calls.Count(x => x == "save"));
            Assert.Equal("cursor-payload", row.Cursor);
            Assert.Equal(42, row.ProcessedCount);
        }

        [Fact]
        public async Task Failing_Notifications_Are_Logged_Once_Per_Run_With_A_Count()
        {
            var fixture = new Fixture();
            var failure = new TimeoutException("send failed");
            fixture.SendThrows(failure);
            fixture.ImportReports(new ImportProgressInfo(), new ImportProgressInfo(), new ImportProgressInfo());

            await fixture.Service.RunImportAsync(new ImportProfile(), new ImportPushNotification("tester"), CancellationToken.None);

            // 3 progress + 1 closing = 4 failures: the first is logged, the other 3 are counted
            var errors = fixture.Logger.Entries.Where(x => x.Level == LogLevel.Error).ToList();
            Assert.Equal(2, errors.Count);
            Assert.Same(failure, errors[0].Exception);
            Assert.Contains("3 more push notifications", errors[1].Message);
        }

        [Fact]
        public async Task Replay_Of_An_Unfinished_Run_Closes_Its_Row_And_Starts_A_New_One()
        {
            var interrupted = new ImportRunHistory
            {
                Id = "H1",
                JobId = "job-1",
                Cursor = "dummy-cursor",
                Errors = new List<string> { "Line 5: b" },
                ErrorsCount = 1,
            };
            var fixture = new Fixture();
            fixture.LatestRowOfJob("job-1", interrupted);

            await fixture.Service.RunImportAsync(new ImportProfile(), new ImportPushNotification("tester") { JobId = "job-1" }, CancellationToken.None);

            // Saves: the closed row, the start of the new run, the finished new run
            Assert.Equal(3, fixture.SavedRows.Count);
            Assert.Same(interrupted, fixture.SavedRows[0]);
            Assert.NotNull(fixture.SavedFinished[0]);
            Assert.Null(interrupted.Cursor);
            Assert.Equal(2, fixture.SavedErrors[0].Count);
            Assert.Contains("interrupted", fixture.SavedErrors[0][0]);
            Assert.Equal("Line 5: b", fixture.SavedErrors[0][1]);
            Assert.Equal(2, interrupted.ErrorsCount);
            Assert.NotSame(interrupted, fixture.SavedRows[1]);
            Assert.Same(fixture.SavedRows[1], fixture.SavedRows[2]);
            Assert.Null(fixture.SavedRows[1].Cursor);
            Assert.DoesNotContain("Line 5: b", fixture.SavedErrors[1] ?? []);
        }

        [Fact]
        public async Task Replay_Of_A_Resumable_Run_Closes_Nothing_And_Continues_On_Its_Row()
        {
            var resumable = new ImportRunHistory
            {
                Id = "H1",
                JobId = "job-1",
                Finished = DateTime.UtcNow,
                Cursor = "dummy-cursor",
                Errors = new List<string> { "Line 5: b" },
            };
            var fixture = new Fixture();
            fixture.LatestRowOfJob("job-1", resumable);

            await fixture.Service.RunImportAsync(new ImportProfile(), new ImportPushNotification("tester") { JobId = "job-1" }, CancellationToken.None);

            // Saves: the start of the resumed run, the finished run; both on the one row, the interrupted-run error absent
            Assert.Equal(2, fixture.SavedRows.Count);
            Assert.All(fixture.SavedRows, x => Assert.Same(resumable, x));
            Assert.All(fixture.SavedErrors, x => Assert.DoesNotContain(x, y => y.Contains("interrupted")));
        }

        [Fact]
        public async Task Replay_Of_A_Finished_Run_Without_A_Cursor_Closes_Nothing_And_Starts_A_New_One()
        {
            var finished = new ImportRunHistory
            {
                Id = "H1",
                JobId = "job-1",
                Finished = DateTime.UtcNow,
                Cursor = null,
                Errors = new List<string> { "Line 5: b" },
            };
            var fixture = new Fixture();
            fixture.LatestRowOfJob("job-1", finished);

            await fixture.Service.RunImportAsync(new ImportProfile(), new ImportPushNotification("tester") { JobId = "job-1" }, CancellationToken.None);

            // Saves: the start of the new run, the finished new run; the finished row is never saved
            Assert.Equal(2, fixture.SavedRows.Count);
            Assert.DoesNotContain(finished, fixture.SavedRows);
            Assert.All(fixture.SavedErrors, x => Assert.DoesNotContain(x ?? [], y => y.Contains("interrupted")));
        }

        [Fact]
        public async Task Replay_Without_A_Row_Of_The_Job_Closes_Nothing()
        {
            var fixture = new Fixture();
            fixture.LatestRowOfJob("job-1", null);

            await fixture.Service.RunImportAsync(new ImportProfile(), new ImportPushNotification("tester") { JobId = "job-1" }, CancellationToken.None);

            // Saves: the start of the run, the finished run
            Assert.Equal(2, fixture.SavedRows.Count);
            Assert.Same(fixture.SavedRows[0], fixture.SavedRows[1]);
        }

        [Fact]
        public async Task Run_Without_A_Job_Id_Neither_Searches_Nor_Closes()
        {
            var fixture = new Fixture();
            fixture.LatestRowOfJob("job-1", new ImportRunHistory { Id = "H1", JobId = "job-1" });

            await fixture.Service.RunImportAsync(new ImportProfile(), new ImportPushNotification("tester"), CancellationToken.None);

            // Saves: the start of the run, the finished run
            Assert.Equal(2, fixture.SavedRows.Count);
            fixture.Search.Verify(x => x.SearchAsync(It.IsAny<SearchImportRunHistoryCriteria>(), It.IsAny<bool>()), Times.Never);
        }

        private sealed class Fixture
        {
            private readonly Mock<IDataImportProcessManager> _manager = new();
            private readonly Mock<IPushNotificationManager> _pushManager = new();
            private readonly Mock<UserManager<ApplicationUser>> _userManager = new(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            // Strict, so no member other than the one CompletionEmailFails sets up can be called
            private readonly Mock<INotificationSearchService> _notificationSearch = new(MockBehavior.Strict);
            private Exception _saveFailure;
            private int _savesBeforeFailure;

            public Fixture()
            {
                var historyCrud = new Mock<IImportRunHistoryCrudService>();
                historyCrud.Setup(x => x.SaveChangesAsync(It.IsAny<IList<ImportRunHistory>>()))
                    .Callback<IList<ImportRunHistory>>(x =>
                    {
                        Calls.Add("save");
                        SavedRows.Add(x[0]);
                        SavedErrors.Add(x[0].Errors?.ToList());
                        SavedFinished.Add(x[0].Finished);
                        SavedCounts.Add((x[0].ProcessedCount, x[0].TotalCount));
                    })
                    .Returns(() => _saveFailure is not null && Calls.Count(x => x == "save") > _savesBeforeFailure
                        ? Task.FromException(_saveFailure)
                        : Task.CompletedTask);

                _pushManager.Setup(x => x.SendAsync(It.IsAny<PushNotification>()))
                    .Callback<PushNotification>(_ => Calls.Add("send"))
                    .Returns(Task.CompletedTask);

                _manager.Setup(x => x.ImportAsync(It.IsAny<ImportProfile>(), It.IsAny<Func<ImportProgressInfo, Task>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                // No user means no completion e-mail
                _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);

                Service = new TestableRunImportService(historyCrud.Object, Search.Object, _manager.Object, _pushManager.Object, Logger, _userManager.Object, _notificationSearch.Object);
            }

            public TestableRunImportService Service { get; }

            public RecordingLogger<ImportRunService> Logger { get; } = new();

            public Mock<IImportRunHistorySearchService> Search { get; } = new();

            public List<string> Calls { get; } = [];

            public List<ImportRunHistory> SavedRows { get; } = [];

            public List<List<string>> SavedErrors { get; } = [];

            public List<DateTime?> SavedFinished { get; } = [];

            public List<(int Processed, int Total)> SavedCounts { get; } = [];

            // The latest row the search finds for the job (none when `row` is null)
            public void LatestRowOfJob(string jobId, ImportRunHistory row)
            {
                Search.Setup(x => x.SearchAsync(It.Is<SearchImportRunHistoryCriteria>(c => c.JobId == jobId), It.IsAny<bool>()))
                    .ReturnsAsync(new SearchImportRunHistoryResult
                    {
                        Results = row is not null ? new[] { row } : Array.Empty<ImportRunHistory>(),
                        TotalCount = row is not null ? 1 : 0,
                    });
            }

            // Saves after the first `successfulSaves` ones throw (the run's first save happens before the import starts).
            public void SaveThrowsAfter(int successfulSaves, Exception exception)
            {
                _savesBeforeFailure = successfulSaves;
                _saveFailure = exception;
            }

            // The creator resolves to a user with an e-mail address, so the completion e-mail is attempted; its
            // notification search throws the given exception.
            public void CompletionEmailFails(Exception exception)
            {
                _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
                    .ReturnsAsync(new ApplicationUser { UserName = "tester", Email = "tester@example.com" });
                _notificationSearch.Setup(x => x.SearchNotificationsAsync(It.IsAny<NotificationSearchCriteria>()))
                    .ThrowsAsync(exception);
            }

            public void ImportThrows(Exception exception)
            {
                _manager.Setup(x => x.ImportAsync(It.IsAny<ImportProfile>(), It.IsAny<Func<ImportProgressInfo, Task>>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(exception);
            }

            public void ImportReports(params ImportProgressInfo[] progress)
            {
                _manager.Setup(x => x.ImportAsync(It.IsAny<ImportProfile>(), It.IsAny<Func<ImportProgressInfo, Task>>(), It.IsAny<CancellationToken>()))
                    .Returns<ImportProfile, Func<ImportProgressInfo, Task>, CancellationToken>(async (_, callback, _) =>
                    {
                        foreach (var info in progress)
                        {
                            await callback(info);
                        }
                    });
            }

            public void SendThrows(Exception exception)
            {
                _pushManager.Setup(x => x.SendAsync(It.IsAny<PushNotification>()))
                    .Callback<PushNotification>(_ => Calls.Add("send"))
                    .ThrowsAsync(exception);
            }
        }

        private sealed class TestableRunImportService : ImportRunService
        {
            public TestableRunImportService(IImportRunHistoryCrudService historyCrud, IImportRunHistorySearchService historySearch, IDataImportProcessManager manager, IPushNotificationManager pushManager, ILogger<ImportRunService> logger, UserManager<ApplicationUser> userManager, INotificationSearchService notificationSearch)
                : base(
                    /* UserManager */                    userManager,
                    /* IUserNameResolver */              null!,
                    /* IMemberService */                 null!,
                    /* IBackgroundJobExecutor */         null!,
                    /* IPushNotificationManager */       pushManager,
                    /* INotificationSearchService */     notificationSearch,
                    /* INotificationSender */            null!,
                    /* IImportProfileCrudService */      null!,
                    /* IImportRunHistoryCrudService */   historyCrud,
                    /* IImportRunHistorySearchService */ historySearch,
                    /* IDataImporterFactory */           null!,
                    /* IDataImportProcessManager */      manager,
                    /* ILogger<ImportRunService> */      logger)
            {
            }
        }
    }
}
