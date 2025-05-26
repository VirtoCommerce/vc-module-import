using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Notifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.CsvHelper;
using VirtoCommerce.ImportModule.Data.BackgroundJobs;
using VirtoCommerce.ImportModule.Data.MySql;
using VirtoCommerce.ImportModule.Data.PostgreSql;
using VirtoCommerce.ImportModule.Data.Repositories;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.ImportModule.Data.SqlServer;
using VirtoCommerce.ImportModule.Web.Authorization;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.TemplateLoader.FileSystem;
using VirtoCommerce.Platform.Core.JsonConverters;
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
            // Database initialization
            serviceCollection.AddDbContext<ImportDbContext>((_, options) =>
            {
                var databaseProvider = Configuration.GetValue("DatabaseProvider", "SqlServer");
                var connectionString = Configuration.GetConnectionString(ModuleInfo.Id) ?? Configuration.GetConnectionString("VirtoCommerce");

                switch (databaseProvider)
                {
                    case "MySql":
                        options.UseMySqlDatabase(connectionString);
                        break;
                    case "PostgreSql":
                        options.UsePostgreSqlDatabase(connectionString);
                        break;
                    default:
                        options.UseSqlServerDatabase(connectionString);
                        break;
                }
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

            serviceCollection.AddSingleton<ImportRemainingEstimatorRegistrar>();
            serviceCollection.AddSingleton<IImportRemainingEstimatorFactory>(provider => provider.GetService<ImportRemainingEstimatorRegistrar>());
            serviceCollection.AddSingleton<IImportRemainingEstimatorRegistrar>(provider => provider.GetService<ImportRemainingEstimatorRegistrar>());

            serviceCollection.AddTransient<DefaultRemainingEstimator>();
            serviceCollection.AddTransient<LinearRegressionRemainingEstimator>();

            serviceCollection.AddSingleton<ImportReporterRegistrar>();
            serviceCollection.AddSingleton<IImportReporterFactory>(provider => provider.GetService<ImportReporterRegistrar>());
            serviceCollection.AddSingleton<IImportReporterRegistrar>(provider => provider.GetService<ImportReporterRegistrar>());

            serviceCollection.AddTransient<DefaultDataReporter>();
            serviceCollection.AddTransient<CsvDataReporter>();

            serviceCollection.AddTransient<IDataImportProcessManager, DataImportProcessManager>();

            serviceCollection.AddTransient<IBackgroundJobExecutor, BackgroundJobExecutor>();

            serviceCollection.AddTransient<IAuthorizationHandler, ImportAuthorizationHandler>();

        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            // Prepare settings
            ModuleConstants.Settings.General.RemainingEstimator.DefaultValue = nameof(DefaultRemainingEstimator);
            ModuleConstants.Settings.General.RemainingEstimator.AllowedValues = new object[] { nameof(DefaultRemainingEstimator), nameof(LinearRegressionRemainingEstimator) };

            // Register settings
            var settingsRegistrar = appBuilder.ApplicationServices.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);

            // Register permissions
            var permissionsProvider = appBuilder.ApplicationServices.GetRequiredService<IPermissionsRegistrar>();
            permissionsProvider.RegisterPermissions(ModuleConstants.Security.Permissions.AllPermissions.Select(x =>
                new Permission()
                {
                    GroupName = "Import",
                    ModuleId = ModuleInfo.Id,
                    Name = x
                }).ToArray());


            // Register remaining estimators
            var remainingEstimatorRegistrar = appBuilder.ApplicationServices.GetService<IImportRemainingEstimatorRegistrar>();

            remainingEstimatorRegistrar.Register<DefaultRemainingEstimator>(() => appBuilder.ApplicationServices
                .GetService<DefaultRemainingEstimator>());

            remainingEstimatorRegistrar.Register<LinearRegressionRemainingEstimator>(() => appBuilder.ApplicationServices
                .GetService<LinearRegressionRemainingEstimator>());

            // Register reporters
            var reporterRegistrar = appBuilder.ApplicationServices.GetService<IImportReporterRegistrar>();

            reporterRegistrar.Register<DefaultDataReporter>(() => appBuilder.ApplicationServices
                .GetService<DefaultDataReporter>());

            reporterRegistrar.Register<CsvDataReporter>(() => appBuilder.ApplicationServices
                .GetService<CsvDataReporter>());

            var notificationRegistrar = appBuilder.ApplicationServices.GetService<INotificationRegistrar>();
            var defaultTemplatesDirectory = Path.Combine(ModuleInfo.FullPhysicalPath, "NotificationTemplates");
            notificationRegistrar.RegisterNotification<ImportCompletedEmailNotification>().WithTemplatesFromPath(defaultTemplatesDirectory);

            PolymorphJsonConverter.RegisterTypeForDiscriminator(typeof(ImportProfile), nameof(ImportProfile.ProfileType));

            // Ensure that any pending migrations are applied
            using (var serviceScope = appBuilder.ApplicationServices.CreateScope())
            {
                using (var dbContext = serviceScope.ServiceProvider.GetRequiredService<ImportDbContext>())
                {
                    dbContext.Database.EnsureCreated();
                    dbContext.Database.Migrate();
                }
            }

            // import-app
            var importAppPath = Path.Combine(ModuleInfo.FullPhysicalPath, "App", "dist");
            if (Directory.Exists(importAppPath))
            {
                appBuilder.UseDefaultFiles(new DefaultFilesOptions()
                {
                    FileProvider = new PhysicalFileProvider(importAppPath),
                    RequestPath = new PathString($"/apps/import-app")
                });
                appBuilder.UseStaticFiles(new StaticFileOptions()
                {
                    FileProvider = new PhysicalFileProvider(importAppPath),
                    RequestPath = new PathString($"/apps/import-app")
                });
            }
            ;
        }

        public void Uninstall()
        {
            // do nothing in here
        }
    }
}
