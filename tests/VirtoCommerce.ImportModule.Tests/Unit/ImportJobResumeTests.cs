using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Moq;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Models.Search;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.BackgroundJobs;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportJobResumeTests
    {
        [Fact]
        public async Task Attaches_existing_incomplete_history_and_clears_Finished()
        {
            // Arrange
            var existing = new ImportRunHistory
            {
                Id = "h1",
                JobId = "job-42",
                Finished = DateTime.UtcNow,
                TotalCount = 1000,
                ProcessedCount = 500,
            };
            var (job, run, profile, notification) = CreateJob(existing, "job-42");
            var context = MakePerformContext("job-42");

            // Act
            await job.ImportBackgroundAsync(profile, notification, new JobCancellationToken(false), context);

            // Assert
            Assert.Equal("job-42", notification.JobId);
            Assert.Same(existing, profile.RunHistory);
            Assert.Null(profile.RunHistory.Finished);
            run.Verify(x => x.RunImportAsync(profile, notification, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Ignores_completed_history()
        {
            // Arrange
            var existing = new ImportRunHistory
            {
                Id = "h1",
                JobId = "job-42",
                Finished = DateTime.UtcNow,
                TotalCount = 1000,
                ProcessedCount = 1000,
            };
            var (job, _, profile, notification) = CreateJob(existing, "job-42");
            var context = MakePerformContext("job-42");

            // Act
            await job.ImportBackgroundAsync(profile, notification, new JobCancellationToken(false), context);

            // Assert
            Assert.Null(profile.RunHistory);
        }

        [Fact]
        public async Task No_match_proceeds_as_fresh_run()
        {
            // Arrange
            var (job, _, profile, notification) = CreateJob(existing: null, "job-42");
            var context = MakePerformContext("job-42");

            // Act
            await job.ImportBackgroundAsync(profile, notification, new JobCancellationToken(false), context);

            // Assert
            Assert.Null(profile.RunHistory);
        }

        private static (ImportJob job, Mock<IImportRunService> run, ImportProfile profile, ImportPushNotification notification) CreateJob(ImportRunHistory existing, string jobId)
        {
            var run = new Mock<IImportRunService>();
            var history = new Mock<IImportRunHistorySearchService>();

            var result = new SearchImportRunHistoryResult
            {
                Results = existing is not null ? new[] { existing } : Array.Empty<ImportRunHistory>(),
                TotalCount = existing is not null ? 1 : 0,
            };

            history
                .Setup(x => x.SearchAsync(It.Is<SearchImportRunHistoryCriteria>(c => c.JobId == jobId), It.IsAny<bool>()))
                .ReturnsAsync(result);

            var job = new ImportJob(run.Object, history.Object);
            var profile = new ImportProfile { Name = "P1" };
            var notification = new ImportPushNotification("tester");
            return (job, run, profile, notification);
        }

        private static PerformContext MakePerformContext(string jobId)
        {
            var method = typeof(ImportJob).GetMethod(nameof(ImportJob.ImportBackgroundAsync));
            var hangfireJob = new Job(method, new object[] { null, null, null, null });
            var backgroundJob = new BackgroundJob(jobId, hangfireJob, DateTime.UtcNow);
            var connection = new Mock<IStorageConnection>().Object;
            return new PerformContext(null, connection, backgroundJob, new JobCancellationToken(false));
        }
    }
}
