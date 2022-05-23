using System.Linq;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportModule.Data;
using VirtoCommerce.ImportModule.Data.Importers;
using VirtoCommerce.ImportModule.Data.Importers.ShopifyProductImporter;
using VirtoCommerce.ImportModule.Data.Queries.SearchImportProfiles;
using VirtoCommerce.ImportModule.Data.Queries.SearchImportProfilesHistory;
using VirtoCommerce.ImportModule.Data.Repositories;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Web
{
    public class Module : IModule, IHasConfiguration
    {
        public ManifestModuleInfo ModuleInfo { get; set; }
        public IConfiguration Configuration { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            // database initialization
            serviceCollection.AddDbContext<ImportDbContext>((provider, options) =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                options.UseSqlServer(configuration.GetConnectionString(ModuleInfo.Id) ?? configuration.GetConnectionString("VirtoCommerce"));
            });

            serviceCollection.AddTransient<ICrudService<ImportProfile>, ImportProfileCrudService>();
            serviceCollection.AddTransient<ISearchImportProfilesService, SearchImportProfilesService>();
            serviceCollection.AddTransient<IImportRunHistorySearchService, ImportRunHistorySearchService>();

            serviceCollection.AddSingleton<DataImporterRegistrar>();
            serviceCollection.AddSingleton<IDataImporterFactory>(provider => provider.GetService<DataImporterRegistrar>());
            serviceCollection.AddSingleton<IDataImporterRegistrar>(provider => provider.GetService<DataImporterRegistrar>());
            serviceCollection.AddTransient<IDataImportProcessManager, DataImportProcessManager>();
            serviceCollection.AddTransient<CsvProductImporter>();
            serviceCollection.AddTransient<CsvOfferImporter>();
            serviceCollection.AddTransient<CsvProductImageImporter>();
            serviceCollection.AddTransient<TestImporter>();
            serviceCollection.AddTransient<ShopifyProductImporter>();
            serviceCollection.AddTransient<PropertyMetadataLoader>();

            serviceCollection.AddMediatR(typeof(Anchor));
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            // register settings
            var settingsRegistrar = appBuilder.ApplicationServices.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);

            // register permissions
            var permissionsProvider = appBuilder.ApplicationServices.GetRequiredService<IPermissionsRegistrar>();
            permissionsProvider.RegisterPermissions(ModuleConstants.Security.Permissions.AllPermissions.Select(x =>
                new Permission()
                {
                    GroupName = "Import",
                    ModuleId = ModuleInfo.Id,
                    Name = x
                }).ToArray());

            // Ensure that any pending migrations are applied
            using (var serviceScope = appBuilder.ApplicationServices.CreateScope())
            {
                using (var dbContext = serviceScope.ServiceProvider.GetRequiredService<ImportDbContext>())
                {
                    dbContext.Database.EnsureCreated();
                    dbContext.Database.Migrate();
                }
            }

            //Importers
            var importerRegistrar = appBuilder.ApplicationServices.GetService<IDataImporterRegistrar>();
            importerRegistrar.Register<CsvProductImporter>(() => appBuilder.ApplicationServices.GetService<CsvProductImporter>()).WithSettings(CsvProductSettings.AllSettings);
            importerRegistrar.Register<CsvOfferImporter>(() => appBuilder.ApplicationServices.GetService<CsvOfferImporter>()).WithSettings(CsvSettings.AllSettings);
            importerRegistrar.Register<TestImporter>(() => appBuilder.ApplicationServices.GetService<TestImporter>()).WithSettings(TestSettings.AllSettings);
            importerRegistrar.Register<CsvProductImageImporter>(() => appBuilder.ApplicationServices.GetService<CsvProductImageImporter>()).WithSettings(ProductImageImporterSettings.AllSettings);
            importerRegistrar.Register<ShopifyProductImporter>(() => appBuilder.ApplicationServices.GetService<ShopifyProductImporter>()).WithSettings(ShopifyProductSettings.AllSettings);

        }

        public void Uninstall()
        {
            // do nothing in here
        }
    }
}
