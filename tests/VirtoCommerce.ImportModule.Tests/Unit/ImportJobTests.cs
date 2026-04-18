using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Moq;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.BackgroundJobs;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportJobTests
    {
        [Fact]
        public async Task ImportBackgroundAsync_delegates_to_run_service()
        {
            // Arrange
            var runService = new Mock<IImportRunService>();
            var importJob = new ImportJob(runService.Object);
            var profile = new ImportProfile { Name = "P1" };
            var notification = new ImportPushNotification("tester");

            // Act
            await importJob.ImportBackgroundAsync(profile, notification, new JobCancellationToken(false), null);

            // Assert
            runService.Verify(x => x.RunImportAsync(profile, notification, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
