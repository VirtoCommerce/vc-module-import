using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Models.Search;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Services;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportRunServiceAttachTests
    {
        [Fact]
        public async Task Attaches_Resumable_History_And_Clears_Finished()
        {
            var existing = new ImportRunHistory
            {
                Id = "h1",
                JobId = "job-42",
                Finished = DateTime.UtcNow,
                TotalCount = 1000,
                ProcessedCount = 500,
                Cursor = "dummy-cursor",
            };
            var service = CreateService(existing, "job-42");
            var result = await service.TryGetRunHistoryAsyncPublic(new ImportProfile(), new ImportPushNotification("tester") { JobId = "job-42" });

            Assert.Same(existing, result);
            Assert.Null(result.Finished);
        }

        [Fact]
        public async Task Attaches_Completed_History_With_Saved_Cursor()
        {
            // Lenient gate: a finished run with a cursor is resumable even when ProcessedCount
            // equals TotalCount. Re-running the last batch is harmless for idempotent writers
            // and lets users replay the tail of a successful import on demand.
            var completed = new ImportRunHistory
            {
                Id = "h1",
                JobId = "job-42",
                Finished = DateTime.UtcNow,
                TotalCount = 1000,
                ProcessedCount = 1000,
                Cursor = "dummy-cursor",
            };
            var service = CreateService(completed, "job-42");

            var result = await service.TryGetRunHistoryAsyncPublic(new ImportProfile(), new ImportPushNotification("tester") { JobId = "job-42" });

            Assert.Same(completed, result);
            Assert.Null(result.Finished);
        }

        [Fact]
        public async Task Returns_Null_For_Finished_History_Without_Cursor()
        {
            // No cursor → nothing to replay → not resumable.
            var completed = new ImportRunHistory
            {
                Id = "h1",
                JobId = "job-42",
                Finished = DateTime.UtcNow,
                TotalCount = 1000,
                ProcessedCount = 1000,
                Cursor = null,
            };
            var service = CreateService(completed, "job-42");

            var result = await service.TryGetRunHistoryAsyncPublic(new ImportProfile(), new ImportPushNotification("tester") { JobId = "job-42" });

            Assert.Null(result);
        }

        [Fact]
        public async Task Returns_Null_When_Search_Finds_Nothing()
        {
            var service = CreateService(existing: null, "job-42");

            var result = await service.TryGetRunHistoryAsyncPublic(new ImportProfile(), new ImportPushNotification("tester") { JobId = "job-42" });

            Assert.Null(result);
        }

        [Fact]
        public async Task Returns_Null_When_JobId_Is_Empty()
        {
            var service = CreateService(existing: null, "whatever");

            var result = await service.TryGetRunHistoryAsyncPublic(new ImportProfile(), new ImportPushNotification("tester") { JobId = null });

            Assert.Null(result);
        }

        private static TestableAttachService CreateService(ImportRunHistory existing, string expectedJobId)
        {
            var search = new Mock<IImportRunHistorySearchService>();
            var searchResult = new SearchImportRunHistoryResult
            {
                Results = existing is not null ? new[] { existing } : Array.Empty<ImportRunHistory>(),
                TotalCount = existing is not null ? 1 : 0,
            };
            search
                .Setup(x => x.SearchAsync(It.Is<SearchImportRunHistoryCriteria>(c => c.JobId == expectedJobId), It.IsAny<bool>()))
                .ReturnsAsync(searchResult);

            return new TestableAttachService(search.Object);
        }

        private sealed class TestableAttachService : ImportRunService
        {
            public TestableAttachService(IImportRunHistorySearchService searchService)
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
                    /* IImportRunHistorySearchService */ searchService,
                    /* IDataImporterFactory */           null!,
                    /* IDataImportProcessManager */      null!,
                    /* ILogger<ImportRunService> */      NullLogger<ImportRunService>.Instance)
            {
            }

            public Task<ImportRunHistory> TryGetRunHistoryAsyncPublic(ImportProfile profile, ImportPushNotification notification)
                => TryGetRunHistoryAsync(profile, notification);
        }
    }
}
