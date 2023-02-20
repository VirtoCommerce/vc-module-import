using Microsoft.Extensions.DependencyInjection;
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

            var importRemainingEstimatorRegistar = new ImportRemainingEstimatorRegistar(provider);
            importRemainingEstimatorRegistar.Register<TestRemainingEstimator>(() => new TestRemainingEstimator());

            var importReporterRegistrar = new ImportReporterRegistrar(provider);
            importReporterRegistrar.Register<TestDataReporter>(() => new TestDataReporter());

            var settingsManager = provider.GetService<ISettingsManager>();
            var result = new DataImportProcessManager(dataImporterRegistrar, importRemainingEstimatorRegistar, importReporterRegistrar, settingsManager);
            return result;
        }

        public static string GetFilePath(string fileName)
        {
            return $"../../../Unit/data/{fileName}";
        }
    }
}
