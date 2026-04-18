using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Tests
{
    public static class TestHepler
    {
        public static DataImportProcessManager GetDataImportProcessManager()
        {
            var dataImporterFactory = new Mock<IDataImporterFactory>();
            dataImporterFactory.Setup(x => x.Create(It.IsAny<string>())).Returns(new Mock<IDataImporter>().Object);

            return new DataImportProcessManager(
                dataImporterFactory.Object,
                new Mock<IImportRemainingEstimatorFactory>().Object,
                new Mock<IImportReporterFactory>().Object,
                new Mock<ISettingsManager>().Object,
                NullLoggerFactory.Instance);
        }
    }
}
