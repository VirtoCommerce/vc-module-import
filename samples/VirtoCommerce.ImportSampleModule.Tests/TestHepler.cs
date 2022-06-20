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
            var registrar = new DataImporterRegistrar(provider);
            registrar.Register<TestImporter>(() => new TestImporter());

            var settingsManager = provider.GetService<ISettingsManager>();
            var result = new DataImportProcessManager(registrar, settingsManager);
            return result;
        }

        public static string GetFilePath(string fileName)
        {
            return $"../../../Unit/data/{fileName}";
        }
    }
}
