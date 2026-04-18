using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Services;
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
    }
}
