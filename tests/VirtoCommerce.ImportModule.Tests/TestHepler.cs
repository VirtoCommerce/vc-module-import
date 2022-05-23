using System;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.CatalogModule.Data.Repositories;
using VirtoCommerce.CatalogModule.Data.Search;
using VirtoCommerce.CatalogModule.Data.Search.BrowseFilters;
using VirtoCommerce.CatalogModule.Data.Search.Indexing;
using VirtoCommerce.CatalogModule.Data.Services;
using VirtoCommerce.CatalogModule.Data.Validation;
using VirtoCommerce.ImportModule.Data;
using VirtoCommerce.ImportModule.Data.Importers;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.MarketplaceVendorModule.Data.BackgroundJobs;
using VirtoCommerce.MarketplaceVendorModule.Data.Infrastructure;
using VirtoCommerce.MarketplaceVendorModule.Data.Queries;
using VirtoCommerce.MarketplaceVendorModule.Data.Services;
using VirtoCommerce.MarketplaceVendorModule.Data.Validators;
using VirtoCommerce.MarketplaceVendorModule.Tests.Functional;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Bus;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.Repositories;
using VirtoCommerce.Platform.Data.Settings;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.SearchModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ImportModule.Tests
{
    public static class TestHepler
    {
        public static string GetFilePath(string fileName)
        {
            return $"../../../Unit/data/{fileName}";
        }

        public static DataImportProcessManager GetDataImportProcessManager()
        {
            var services = new ServiceCollection();
            var provider = services.BuildServiceProvider();
            var registrar = new DataImporterRegistrar(provider);
            registrar.Register<TestImporter>(() => new TestImporter());
            var result = new DataImportProcessManager(registrar);
            return result;
        }

        public static T GetService<T>()
        {
            var services = new ServiceCollection();
            services.AddTransient<ICrudService<SellerProduct>, SellerProductsCrudService>();
            services.AddTransient<ICrudService<ProductPublicationRequest>, PublicationRequestCrudService>();
            services.AddTransient<ISellerRepository, SellerRepositoryMock>();
            services.AddTransient<Func<ISellerRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetRequiredService<ISellerRepository>());
            services.AddTransient<IPlatformMemoryCache, PlatformMemoryCache>();
            services.AddTransient<IMemoryCache, MemoryCache>();
            services.AddLogging();
            services.AddTransient<IEventPublisher, InProcessBus>();
            services.AddTransient<IItemService, ItemServiceStub>();
            services.AddTransient<ICategoryService, CategoryServiceStub>();
            services.AddTransient<IPushNotificationManager, PushNotificationManagerStub>();
            services.AddOptions();
            services.AddTransient<ILoggerFactory, LoggerFactory>();
            services.AddTransient<IIndexingJobExecutor, IndexingJobExecutorStub>();
            services.AddTransient<SellerProductValidator>();
            services.AddTransient<ISellerProductsSearchService, SellerProductsSearchServiceStub>();
            services.AddSingleton<ISearchProvider, DummySearchProvider>();
            services.AddSingleton<ISearchRequestBuilderRegistrar, SearchRequestBuilderRegistrar>();
            services.AddTransient<IProductIndexedSearchService, ProductIndexedSearchService>();
            services.AddTransient<ISettingsManager, SettingsManager>();
            services.AddTransient<IPlatformRepository, PlatformRepository>();
            services.AddTransient<Func<IPlatformRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetService<IPlatformRepository>());
            services.AddSingleton<IBlobUrlResolver, BlobUrlResolverStub>();
            services.AddTransient<IAggregationConverter, AggregationConverter>();
            services.AddTransient<IBrowseFilterService, BrowseFilterService>();
            services.AddTransient(x => (IStoreService)x.GetRequiredService<ICrudService<Store>>());
            services.AddTransient<IPropertyService, PropertyService>();
            services.AddTransient<ICatalogRepository, CatalogRepositoryImpl>();
            services.AddTransient<Func<ICatalogRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetRequiredService<ICatalogRepository>());
            services.AddTransient<IProductSearchService, ProductSearchService>();
            services.AddTransient<ICatalogService, CatalogService>();
            services.AddTransient<ICatalogSearchService, CatalogSearchService>();
            PropertyValueValidator PropertyValueValidatorFactory(PropertyValidationRule rule) => new PropertyValueValidator(rule);
            services.AddSingleton(PropertyValueValidatorFactory);
            services.AddTransient<AbstractValidator<IHasProperties>, HasPropertiesValidator>();
            services.AddTransient<AbstractValidator<Property>, PropertyValidator>();
            services.AddTransient<IPropertyDictionaryItemSearchService, PropertyDictionaryItemSearchService>();
            services.AddTransient<IPropertyDictionaryItemService, PropertyDictionaryItemService>();
            services.AddTransient<ICrudService<Store>, StoreServiceStub>();
            services.AddTransient<IPublicationRequestsSearchService, PublicationRequestsSearchService>();
            services.AddMediatR(typeof(Anchor));
            var provider = services.BuildServiceProvider();
            var service = provider.GetService<T>();
            return service;
        }
    }
}
