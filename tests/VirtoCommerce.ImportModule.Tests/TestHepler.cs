using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.ImportModule.Data.Importers;
using VirtoCommerce.ImportModule.Data.Services;

namespace VirtoCommerce.ImportModule.Tests
{
    public static class TestHepler
    {
        public static DataImportProcessManager GetDataImportProcessManager()
        {
            var services = new ServiceCollection();
            var provider = services.BuildServiceProvider();
            var registrar = new DataImporterRegistrar(provider);
            registrar.Register<TestImporter>(() => new TestImporter());
            var result = new DataImportProcessManager(registrar);
            return result;
        }
    }
}
