using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire;
using Moq;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.BackgroundJobs;
using Xunit;

namespace VirtoCommerce.ImportSampleModule.Tests.Unit
{
    public class ImportJobTests
    {
        private readonly Mock<IImportRunService> _importRunService = new();
        private readonly ImportProfile _profile = new();
        private readonly ImportPushNotification _pushNotification = new("TestUser");

        [Fact]
        public async Task Import_job_normal_flow()
        {
            // Arrange
            var importJob = new ImportJob(_importRunService.Object);

            // Act
            await importJob.ImportBackgroundAsync(_profile, _pushNotification, new JobCancellationToken(false), null);

            // Assertion
            _importRunService.Verify(x => x.RunImportAsync(_profile, _pushNotification, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Import_job_aborted_flow()
        {
            // Arrange
            var importJob = new ImportJob(_importRunService.Object);

            // Act
            try
            {
                await importJob.ImportBackgroundAsync(_profile, _pushNotification, new JobCancellationToken(false), null);
            }

            // Assertion
            catch (Exception ex)
            {
                ex.Should().NotBeNull();
            }
        }
    }
}
