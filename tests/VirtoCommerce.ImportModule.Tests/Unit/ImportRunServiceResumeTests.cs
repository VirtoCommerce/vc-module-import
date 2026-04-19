using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.Platform.Core.Security;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportRunServiceResumeTests
    {
        [Fact]
        public async Task ResumeImportAsync_returns_PushNotification_when_history_is_resumable_and_hangfire_accepts()
        {
            var history = Resumable("hist-1", "job-42");
            var service = new TestableResumeService(history, requeueReturns: true, currentUser: "tester");

            var notification = await service.ResumeImportAsync("hist-1");

            Assert.NotNull(notification);
            Assert.Equal("job-42", notification.JobId);
            Assert.Equal(history.ProfileId, notification.ProfileId);
            Assert.Equal(history.ProfileName, notification.ProfileName);
            Assert.Equal(history.ProcessedCount, notification.ProcessedCount);
            Assert.Equal(history.TotalCount, notification.TotalCount);
            Assert.Equal("tester", notification.Creator);
            Assert.Equal("Import process", notification.Title);
            Assert.Equal("job-42", service.RequeuedJobId);
        }

        [Fact]
        public async Task ResumeImportAsync_throws_OperationCanceledException_when_history_not_found()
        {
            var service = new TestableResumeService(history: null, requeueReturns: true);

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => service.ResumeImportAsync("missing"));
            Assert.Null(service.RequeuedJobId);
        }

        [Fact]
        public async Task ResumeImportAsync_throws_InvalidOperationException_when_history_still_running()
        {
            var stillRunning = new ImportRunHistory { Id = "h", JobId = "job", Finished = null, TotalCount = 100, ProcessedCount = 50 };
            var service = new TestableResumeService(stillRunning, requeueReturns: true);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ResumeImportAsync("h"));
            Assert.Null(service.RequeuedJobId);
        }

        [Fact]
        public async Task ResumeImportAsync_throws_InvalidOperationException_when_history_already_completed()
        {
            var completed = new ImportRunHistory { Id = "h", JobId = "job", Finished = DateTime.UtcNow, TotalCount = 100, ProcessedCount = 100 };
            var service = new TestableResumeService(completed, requeueReturns: true);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ResumeImportAsync("h"));
            Assert.Null(service.RequeuedJobId);
        }

        [Fact]
        public async Task ResumeImportAsync_throws_InvalidOperationException_when_history_has_no_JobId()
        {
            var noJob = new ImportRunHistory { Id = "h", JobId = null, Finished = DateTime.UtcNow, TotalCount = 100, ProcessedCount = 42 };
            var service = new TestableResumeService(noJob, requeueReturns: true);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ResumeImportAsync("h"));
            Assert.Null(service.RequeuedJobId);
        }

        [Fact]
        public async Task ResumeImportAsync_throws_InvalidOperationException_when_hangfire_rejects()
        {
            var service = new TestableResumeService(Resumable("h", "job"), requeueReturns: false);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ResumeImportAsync("h"));
            Assert.Equal("job", service.RequeuedJobId);
        }

        private static ImportRunHistory Resumable(string id, string jobId) => new()
        {
            Id = id,
            JobId = jobId,
            ProfileId = "profile-1",
            ProfileName = "Profile One",
            Finished = DateTime.UtcNow,
            TotalCount = 100,
            ProcessedCount = 42,
            ErrorsCount = 0,
        };

        private sealed class TestableResumeService : ImportRunService
        {
            private readonly bool _requeueReturns;

            public TestableResumeService(ImportRunHistory history, bool requeueReturns, string currentUser = "tester")
                : base(
                    /* UserManager */                  null!,
                    /* IUserNameResolver */            BuildUserNameResolver(currentUser),
                    /* IMemberService */               null!,
                    /* IBackgroundJobExecutor */       null!,
                    /* IPushNotificationManager */     null!,
                    /* INotificationSearchService */   null!,
                    /* INotificationSender */          null!,
                    /* IImportProfileCrudService */    null!,
                    /* IImportRunHistoryCrudService */ BuildHistoryCrud(history),
                    /* IDataImporterFactory */         null!,
                    /* IDataImportProcessManager */    null!,
                    /* ILogger<ImportRunService> */    NullLogger<ImportRunService>.Instance)
            {
                _requeueReturns = requeueReturns;
            }

            public string RequeuedJobId { get; private set; }

            protected override bool RequeueHangfireJob(string jobId)
            {
                RequeuedJobId = jobId;
                return _requeueReturns;
            }

            private static IUserNameResolver BuildUserNameResolver(string userName)
            {
                var mock = new Mock<IUserNameResolver>();
                mock.Setup(x => x.GetCurrentUserName()).Returns(userName);
                return mock.Object;
            }

            private static IImportRunHistoryCrudService BuildHistoryCrud(ImportRunHistory history)
            {
                var mock = new Mock<IImportRunHistoryCrudService>();
                IList<ImportRunHistory> result = history is null ? [] : [history];
                mock.Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                    .ReturnsAsync(result);
                return mock.Object;
            }
        }
    }
}
