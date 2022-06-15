using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportSampleModule.Web.Importers;
using VirtoCommerce.ImportSampleModule.Web.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;

namespace VirtoCommerce.ImportSampleModule.Web
{
    public class Module : IModule, IHasConfiguration
    {
        public ManifestModuleInfo ModuleInfo { get; set; }
        public IConfiguration Configuration { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IExtendedProductSearchService, ExtendedProductSearchService>();

            serviceCollection.AddTransient<TestImporter>();
            serviceCollection.AddTransient<CsvProductImporter>();
            serviceCollection.AddTransient<CsvProductImageImporter>();
            serviceCollection.AddTransient<ShopifyProductImporter>();

            AbstractTypeFactory<ProductSearchCriteria>.OverrideType<ProductSearchCriteria, ExtendedProductSearchCriteria>();
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            //Importers
            var importerRegistrar = appBuilder.ApplicationServices.GetService<IDataImporterRegistrar>();
            importerRegistrar.Register<TestImporter>(() => appBuilder.ApplicationServices.GetService<TestImporter>()).WithSettings(TestSettings.AllSettings);
            importerRegistrar.Register<CsvProductImporter>(() => appBuilder.ApplicationServices.GetService<CsvProductImporter>()).WithSettings(CsvProductSettings.AllSettings);
            importerRegistrar.Register<CsvProductImageImporter>(() => appBuilder.ApplicationServices.GetService<CsvProductImageImporter>()).WithSettings(CsvProductImageSettings.AllSettings);
            importerRegistrar.Register<ShopifyProductImporter>(() => appBuilder.ApplicationServices.GetService<ShopifyProductImporter>()).WithSettings(ShopifyProductSettings.AllSettings);
        }

        public void Uninstall()
        {
            // do nothing in here
        }
    }
}
