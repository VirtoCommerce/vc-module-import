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
                    .Returns(Task.CompletedTask);

                _pushManager.Setup(x => x.SendAsync(It.IsAny<PushNotification>()))
                    .Callback<PushNotification>(_ => Calls.Add("send"))
                    .Returns(Task.CompletedTask);

                _manager.Setup(x => x.ImportAsync(It.IsAny<ImportProfile>(), It.IsAny<Func<ImportProgressInfo, Task>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                Service = new TestableRunImportService(historyCrud.Object, _manager.Object, _pushManager.Object, Logger);
            }

            public TestableRunImportService Service { get; }

            public RecordingLogger<ImportRunService> Logger { get; } = new();

            public List<string> Calls { get; } = [];

            public List<List<string>> SavedErrors { get; } = [];

            public List<DateTime?> SavedFinished { get; } = [];

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
            public TestableRunImportService(IImportRunHistoryCrudService historyCrud, IDataImportProcessManager manager, IPushNotificationManager pushManager, ILogger<ImportRunService> logger)
                : base(
                    /* UserManager */                    BuildUserManager(),
                    /* IUserNameResolver */              null!,
                    /* IMemberService */                 null!,
                    /* IBackgroundJobExecutor */         null!,
                    /* IPushNotificationManager */       pushManager,
                    /* INotificationSearchService */     null!,
                    /* INotificationSender */            null!,
                    /* IImportProfileCrudService */      null!,
                    /* IImportRunHistoryCrudService */   historyCrud,
                    /* IImportRunHistorySearchService */ null!,
                    /* IDataImporterFactory */           null!,
                    /* IDataImportProcessManager */      manager,
                    /* ILogger<ImportRunService> */      logger)
            {
            }

            // The finally block looks the creator up; no user means no completion e-mail.
            private static UserManager<ApplicationUser> BuildUserManager()
            {
                var userManager = new Mock<UserManager<ApplicationUser>>(
                    Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
                userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);

                return userManager.Object;
            }
        }
    }
}
