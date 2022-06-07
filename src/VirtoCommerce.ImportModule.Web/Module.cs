using System;
using System.Linq;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data;
using VirtoCommerce.ImportModule.Data.BackgroundJobs;
using VirtoCommerce.ImportModule.Data.Repositories;
using VirtoCommerce.ImportModule.Data.Services;
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

            serviceCollection.AddTransient<IImportRepository, ImportRepository>();
            serviceCollection.AddTransient<Func<IImportRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetRequiredService<IImportRepository>());

            serviceCollection.AddTransient<IImportRunService, ImportRunService>();

            serviceCollection.AddTransient<IImportProfileCrudService, ImportProfileCrudService>();
            serviceCollection.AddTransient<IImportProfilesSearchService, ImportProfilesSearchService>();

            serviceCollection.AddTransient<IImportRunHistoryCrudService, ImportRunHistoryCrudService>();
            serviceCollection.AddTransient<IImportRunHistorySearchService, ImportRunHistorySearchService>();

            serviceCollection.AddSingleton<DataImporterRegistrar>();
            serviceCollection.AddSingleton<IDataImporterFactory>(provider => provider.GetService<DataImporterRegistrar>());
            serviceCollection.AddSingleton<IDataImporterRegistrar>(provider => provider.GetService<DataImporterRegistrar>());
            serviceCollection.AddTransient<IDataImportProcessManager, DataImportProcessManager>();

            //serviceCollection.AddTransient<TestImporter>();

            serviceCollection.AddTransient<IBackgroundJobExecutor, BackgroundJobExecutor>();

            serviceCollection.AddMediatR(typeof(Anchor));
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            // register settings
            var settingsRegistrar = appBuilder.ApplicationServices.GetRequiredService<ISettingsRegistrar>();

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

            ////Importers
            //var importerRegistrar = appBuilder.ApplicationServices.GetService<IDataImporterRegistrar>();
            //importerRegistrar.Register<TestImporter>(() => appBuilder.ApplicationServices.GetService<TestImporter>()).WithSettings(TestSettings.AllSettings);
        }

        public void Uninstall()
        {
            // do nothing in here
        }
    }
}
