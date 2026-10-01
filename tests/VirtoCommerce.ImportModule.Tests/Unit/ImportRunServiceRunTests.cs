using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using VirtoCommerce.ImportModule.Core.Models;
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
            fixture.CompletionEmailFails(new TimeoutException("mail down"));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.Service.RunImportAsync(new ImportProfile(), new ImportPushNotification("tester"), CancellationToken.None));

            Assert.Equal("import failed", exception.Message);
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

        private sealed class Fixture
        {
            private readonly Mock<IDataImportProcessManager> _manager = new();
            private readonly Mock<IPushNotificationManager> _pushManager = new();
            private readonly Mock<UserManager<ApplicationUser>> _userManager = new(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            // Strict: any call on it throws, which stands in for a failing notification search
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
                        SavedErrors.Add(x[0].Errors?.ToList());
                        SavedFinished.Add(x[0].Finished);
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

                Service = new TestableRunImportService(historyCrud.Object, _manager.Object, _pushManager.Object, Logger, _userManager.Object, _notificationSearch.Object);
            }

            public TestableRunImportService Service { get; }

            public RecordingLogger<ImportRunService> Logger { get; } = new();

            public List<string> Calls { get; } = [];

            public List<List<string>> SavedErrors { get; } = [];

            public List<DateTime?> SavedFinished { get; } = [];

            // Saves after the first `successfulSaves` ones throw (the run's first save happens before the import starts).
            public void SaveThrowsAfter(int successfulSaves, Exception exception)
            {
                _savesBeforeFailure = successfulSaves;
                _saveFailure = exception;
            }

            // The creator resolves to a user with an e-mail address, so the completion e-mail is attempted; its
            // notification search is the strict mock above and throws.
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
            public TestableRunImportService(IImportRunHistoryCrudService historyCrud, IDataImportProcessManager manager, IPushNotificationManager pushManager, ILogger<ImportRunService> logger, UserManager<ApplicationUser> userManager, INotificationSearchService notificationSearch)
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
                    /* IImportRunHistorySearchService */ null!,
                    /* IDataImporterFactory */           null!,
                    /* IDataImportProcessManager */      manager,
                    /* ILogger<ImportRunService> */      logger)
            {
            }
        }
    }
}
