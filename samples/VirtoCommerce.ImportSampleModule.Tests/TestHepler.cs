using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.ImportSampleModule.Tests.Functional.Shared;
using VirtoCommerce.ImportSampleModule.Web.Importers;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportSampleModule.Tests
{
    public static class TestHepler
    {
        public static DataImportProcessManager GetDataImportProcessManager()
        {
            var services = new ServiceCollection();
            services.AddTransient<ISettingsManager, SettingsManagerStub>();

            var provider = services.BuildServiceProvider();

            var dataImporterRegistrar = new DataImporterRegistrar(provider);
            dataImporterRegistrar.Register<TestImporter>(() => new TestImporter());

            var importRemainingEstimatorRegistrar = new ImportRemainingEstimatorRegistrar(provider);
            importRemainingEstimatorRegistrar.Register<TestRemainingEstimator>(() => new TestRemainingEstimator());

            var importReporterRegistrar = new ImportReporterRegistrar(provider);
            importReporterRegistrar.Register<TestDataReporter>(() => new TestDataReporter());

            var settingsManager = provider.GetService<ISettingsManager>();

            Mock<ILogger> _loggerMock = new();
            Mock<ILoggerFactory> _loggerFactoryMock = new();
            _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => _loggerMock.Object);

            var result = new DataImportProcessManager(dataImporterRegistrar, importRemainingEstimatorRegistrar, importReporterRegistrar, settingsManager, _loggerFactoryMock.Object);
            return result;
        }

        public static string GetFilePath(string fileName)
        {
            return $"../../../Unit/data/{fileName}";
        }
    }
}
