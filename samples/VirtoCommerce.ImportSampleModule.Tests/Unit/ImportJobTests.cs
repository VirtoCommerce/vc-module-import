using System;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire;
using Moq;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.BackgroundJobs;
using VirtoCommerce.Platform.Core.PushNotifications;
using Xunit;

namespace VirtoCommerce.ImportSampleModule.Tests.Unit
{
    public class ImportJobTests
    {
        private readonly Mock<IPushNotificationManager> _pushNotificationManager = new();
        private readonly Mock<IImportProfileCrudService> _importProfileCrudService = new();
        private readonly Mock<IImportRunHistoryCrudService> _importRunHistoryCrudService = new();
        private readonly ImportProfile profile = new();
        private readonly ImportPushNotification importPushNotifaction = new("TestUser");

        public ImportJobTests()
        {
            _importProfileCrudService.Setup(a => a.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(profile);
        }

        [Fact]
        public async Task Import_job_normal_flow()
        {
            // Arrange
            Mock<IDataImportProcessManager> _dataImportManager = new();
            var importJob = new ImportJob(null);

            // Act
            await importJob.ImportBackgroundAsync(profile, importPushNotifaction, new JobCancellationToken(false), null);

            // Assertion
            _pushNotificationManager.Verify(x => x.Send(It.IsAny<ImportPushNotification>()), Times.Once());
            importPushNotifaction.Finished.Should().HaveValue();
        }

        [Fact]
        public async Task Import_job_aborted_flow()
        {
            // Arrange
            var _dataImportManager = TestHepler.GetDataImportProcessManager();
            var importJob = new ImportJob(null);

            // Act
            try
            {
                await importJob.ImportBackgroundAsync(profile, importPushNotifaction, new JobCancellationToken(false), null);
            }

            // Assertion
            catch (Exception ex)
            {
                ex.Should().NotBeNull();
            }
        }
    }
}
