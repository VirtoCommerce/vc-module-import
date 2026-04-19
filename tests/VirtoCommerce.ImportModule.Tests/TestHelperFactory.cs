using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Tests
{
    internal static class TestHelperFactory
    {
        public static DataImportProcessManager CreateManager(
            IDataImporterFactory factory = null,
            IImportRemainingEstimatorFactory estimator = null,
            IImportReporterFactory reporter = null,
            ISettingsManager settingsManager = null,
            IImportRunHistoryCrudService historyCrud = null,
            ILogger<DataImportProcessManager> logger = null)
        {
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(logger ?? NullLogger<DataImportProcessManager>.Instance);

            return new DataImportProcessManager(
                factory ?? new Mock<IDataImporterFactory>().Object,
                estimator ?? new Mock<IImportRemainingEstimatorFactory>().Object,
                reporter ?? new Mock<IImportReporterFactory>().Object,
                settingsManager ?? new Mock<ISettingsManager>().Object,
                historyCrud ?? new Mock<IImportRunHistoryCrudService>().Object,
                loggerFactory.Object);
        }

        public static DataImportProcessManager CreateManagerWithImporter(
            IImportDataReader reader, IImportDataWriter writer)
        {
            var importer = new Mock<IDataImporter>();
            importer.Setup(x => x.OpenReaderAsync(It.IsAny<ImportContext>())).ReturnsAsync(reader);
            importer.Setup(x => x.OpenWriterAsync(It.IsAny<ImportContext>())).ReturnsAsync(writer);
            importer.Setup(x => x.Clone()).Returns(() => importer.Object);

            var factory = new Mock<IDataImporterFactory>();
            factory.Setup(x => x.Create(It.IsAny<string>())).Returns(importer.Object);

            var estimator = new Mock<IImportRemainingEstimatorFactory>();
            estimator.Setup(x => x.Create(It.IsAny<string>())).Returns(Mock.Of<IImportRemainingEstimator>());
            var reporter = new Mock<IImportReporterFactory>();
            reporter.Setup(x => x.Create(It.IsAny<string>())).Returns(Mock.Of<IImportReporter>());

            return CreateManager(
                factory: factory.Object,
                estimator: estimator.Object,
                reporter: reporter.Object);
        }

        public sealed class TestableRunService : ImportRunService
        {
            public TestableRunService(IImportRunHistoryCrudService historyCrud, IPushNotificationManager pushNotificationManager)
                : base(
                    /* UserManager */                 null!,
                    /* IUserNameResolver */           null!,
                    /* IMemberService */              null!,
                    /* IBackgroundJobExecutor */      null!,
                    /* IPushNotificationManager */    pushNotificationManager,
                    /* INotificationSearchService */  null!,
                    /* INotificationSender */         null!,
                    /* IImportProfileCrudService */   null!,
                    /* IImportRunHistoryCrudService */ historyCrud,
                    /* IDataImporterFactory */        null!,
                    /* IDataImportProcessManager */   null!,
                    /* ILogger<ImportRunService> */   NullLogger<ImportRunService>.Instance)
            {
            }

            public Task InvokeCallbackForTesting(ImportProgressInfo progressInfo, ImportPushNotification pushNotification, ImportRunHistory history)
            {
                return ProgressInfoCallbackImpl(progressInfo, pushNotification, history);
            }
        }

        public static TestableRunService CreateTestableRunService(
            IImportRunHistoryCrudService historyCrud,
            out ImportRunHistory history,
            out ImportPushNotification notif)
        {
            history = new ImportRunHistory { Id = "run-1" };
            notif = new ImportPushNotification("tester");
            var pushManager = new Mock<IPushNotificationManager>();
            pushManager.Setup(x => x.SendAsync(It.IsAny<PushNotification>())).Returns(Task.CompletedTask);
            return new TestableRunService(historyCrud, pushManager.Object);
        }
    }
}
