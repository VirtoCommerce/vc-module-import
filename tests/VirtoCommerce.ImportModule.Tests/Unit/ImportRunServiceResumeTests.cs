using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Models.Search;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Services;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportRunServiceResumeTests
    {
        [Fact]
        public async Task ResumeImportAsync_returns_Resumed_when_history_is_resumable_and_hangfire_accepts()
        {
            var service = new TestableResumeService(Resumable("abc"), requeueReturns: true);

            var result = await service.ResumeImportAsync("abc");

            Assert.Equal(ResumeImportResult.Resumed, result);
            Assert.Equal("abc", service.RequeuedJobId);
        }

        [Fact]
        public async Task ResumeImportAsync_returns_HistoryNotFound_when_search_yields_no_match()
        {
            var service = new TestableResumeService(history: null, requeueReturns: true);

            var result = await service.ResumeImportAsync("abc");

            Assert.Equal(ResumeImportResult.HistoryNotFound, result);
            Assert.Null(service.RequeuedJobId);
        }

        [Fact]
        public async Task ResumeImportAsync_returns_NotResumable_when_history_is_still_running()
        {
            var stillRunning = new ImportRunHistory { JobId = "abc", Finished = null, TotalCount = 100, ProcessedCount = 50 };
            var service = new TestableResumeService(stillRunning, requeueReturns: true);

            var result = await service.ResumeImportAsync("abc");

            Assert.Equal(ResumeImportResult.NotResumable, result);
            Assert.Null(service.RequeuedJobId);
        }

        [Fact]
        public async Task ResumeImportAsync_returns_NotResumable_when_history_is_already_completed()
        {
            var completed = new ImportRunHistory { JobId = "abc", Finished = DateTime.UtcNow, TotalCount = 100, ProcessedCount = 100 };
            var service = new TestableResumeService(completed, requeueReturns: true);

            var result = await service.ResumeImportAsync("abc");

            Assert.Equal(ResumeImportResult.NotResumable, result);
            Assert.Null(service.RequeuedJobId);
        }

        [Fact]
        public async Task ResumeImportAsync_returns_RequeueFailed_when_hangfire_rejects()
        {
            var service = new TestableResumeService(Resumable("abc"), requeueReturns: false);

            var result = await service.ResumeImportAsync("abc");

            Assert.Equal(ResumeImportResult.RequeueFailed, result);
            Assert.Equal("abc", service.RequeuedJobId);
        }

        private static ImportRunHistory Resumable(string jobId) => new()
        {
            JobId = jobId,
            Finished = DateTime.UtcNow,
            TotalCount = 100,
            ProcessedCount = 42,
        };

        private sealed class TestableResumeService : ImportRunService
        {
            private readonly bool _requeueReturns;

            public TestableResumeService(ImportRunHistory history, bool requeueReturns)
                : base(
                    /* UserManager */                    null!,
                    /* IUserNameResolver */              null!,
                    /* IMemberService */                 null!,
                    /* IBackgroundJobExecutor */         null!,
                    /* IPushNotificationManager */       null!,
                    /* INotificationSearchService */     null!,
                    /* INotificationSender */            null!,
                    /* IImportProfileCrudService */      null!,
                    /* IImportRunHistoryCrudService */   null!,
                    /* IImportRunHistorySearchService */ BuildSearchService(history),
                    /* IDataImporterFactory */           null!,
                    /* IDataImportProcessManager */      null!,
                    /* ILogger<ImportRunService> */      NullLogger<ImportRunService>.Instance)
            {
                _requeueReturns = requeueReturns;
            }

            public string RequeuedJobId { get; private set; }

            protected override bool RequeueHangfireJob(string jobId)
            {
                RequeuedJobId = jobId;
                return _requeueReturns;
            }

            private static IImportRunHistorySearchService BuildSearchService(ImportRunHistory history)
            {
                var mock = new Mock<IImportRunHistorySearchService>();
                var result = new SearchImportRunHistoryResult
                {
                    Results = history is null ? new ImportRunHistory[0] : new[] { history },
                    TotalCount = history is null ? 0 : 1,
                };
                mock.Setup(x => x.SearchAsync(It.IsAny<SearchImportRunHistoryCriteria>(), It.IsAny<bool>()))
                    .ReturnsAsync(result);
                return mock.Object;
            }
        }
    }
}
