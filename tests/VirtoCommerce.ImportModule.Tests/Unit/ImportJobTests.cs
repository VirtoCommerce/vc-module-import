using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire;
using Moq;
using VirtoCommerce.ImportModule.Data.BackgroundJobs;
using VirtoCommerce.ImportModule.Data.Commands.RunImport;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains.ImportProfileAggregate.Events;
using VirtoCommerce.MarketplaceVendorModule.Core.PushNotifications;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.PushNotifications;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportJobTests
    {
        private readonly Mock<IPushNotificationManager> _pushNotificationManager = new();
        private readonly Mock<ICrudService<ImportProfile>> _importProfileCrudService = new();
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
            var importDataCommand = new RunImportCommand { ImportProfile = profile };
            Mock<IDataImportProcessManager> _dataImportManager = new();
            var importJob = new ImportJob(_dataImportManager.Object, _pushNotificationManager.Object, _importProfileCrudService.Object);

            // Act
            await importJob.ImportBackgroundAsync(importDataCommand, importPushNotifaction, new JobCancellationToken(false), null);

            // Assertion
            _pushNotificationManager.Verify(x => x.Send(It.IsAny<ImportPushNotification>()), Times.Once());
            importPushNotifaction.Finished.Should().HaveValue();
            importDataCommand.ImportProfile.DomainEvents.Select(x => x.GetType().Name).Should().Contain("ImportFinishedDomainEvent");
            var importFinishedDomainEvent = (ImportFinishedDomainEvent)importDataCommand.ImportProfile.DomainEvents.FirstOrDefault(x => x.GetType().Name == "ImportFinishedDomainEvent");
            importFinishedDomainEvent.Notification.ErrorCount.Should().Be(0);
        }

        [Fact]
        public async Task Import_job_aborted_flow()
        {
            // Arrange
            var importDataCommand = new RunImportCommand { ImportProfile = profile };
            var _dataImportManager = TestHepler.GetDataImportProcessManager();
            var importJob = new ImportJob(_dataImportManager, _pushNotificationManager.Object, _importProfileCrudService.Object);

            // Act
            try
            {
                await importJob.ImportBackgroundAsync(importDataCommand, importPushNotifaction, new JobCancellationToken(false), null);
            }

            // Assertion
            catch (Exception)
            {
                var importFinishedDomainEvent = (ImportFinishedDomainEvent)importDataCommand.ImportProfile.DomainEvents.FirstOrDefault(x => x.GetType().Name == "ImportFinishedDomainEvent");
                importFinishedDomainEvent.Notification.ErrorCount.Should().Be(1);
                importFinishedDomainEvent.Notification.Title.Should().Be("Import failed");
            }
        }
    }
}
