using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.CsvHelper;
using VirtoCommerce.ImportModule.CsvHelper.Services;
using VirtoCommerce.ImportSampleModule.Web.Search;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class CsvProductImporter : IDataImporter, IFileBased
    {
        private readonly IBlobStorageProvider _blobStorageProvider;
        private readonly IExtendedProductSearchService _extendedProductSearchService;
        private readonly PropertyMetadataLoader _propertyMetadataLoader;
        private readonly IItemService _itemService;
        private readonly ICategoryService _categoryService;
        private readonly IServiceProvider _serviceProvider;

        public CsvProductImporter(
            IBlobStorageProvider blobStorageProvider,
            IExtendedProductSearchService extendedProductSearchService,
            PropertyMetadataLoader propertyMetadataLoader,
            IItemService itemService,
            ICategoryService categoryService,
            IServiceProvider serviceProvider
            )
        {
            _blobStorageProvider = blobStorageProvider;
            _extendedProductSearchService = extendedProductSearchService;
            _propertyMetadataLoader = propertyMetadataLoader;
            Metadata = new Dictionary<string, string>()
            {
                { "sampleCsvUrl", "/Modules/$(VirtoCommerce.ImportSampleModule)/Content/seller_product_import_template.csv" },
                { "availableFileExtensions", AvailableFileExtensions }
            };
            _itemService = itemService;
            _categoryService = categoryService;
            _serviceProvider = serviceProvider;
        }

        public virtual string TypeName { get; } = nameof(CsvProductImporter);

        public virtual SettingDescriptor[] AvailSettings { get; set; }

        public virtual Dictionary<string, string> Metadata { get; }

        public IAuthorizationRequirement AuthorizationRequirement { get; set; }

        public string AvailableFileExtensions => ".csv";

        public virtual IImportDataReader OpenReader(ImportContext context)
        {
            if (string.IsNullOrEmpty(context.ImportProfile.ImportFileUrl))
            {
                throw new OperationCanceledException($"Import file must be set");
            }
            var importStream = _blobStorageProvider.OpenRead(context.ImportProfile.ImportFileUrl);

            var classMapRegistrar = _serviceProvider.GetService(typeof(ClassMapRegistrar<CsvProductClassMap, CatalogProduct>)) as ClassMapRegistrar<CsvProductClassMap, CatalogProduct>;
            classMapRegistrar.Register(() => _serviceProvider.GetService(typeof(CsvProductClassMap)) as CsvProductClassMap)
                .WithSettings(context.ImportProfile.Settings);

            return new CsvDataReader<CatalogProduct, CsvProductClassMap>(importStream, context);
        }

        public virtual IImportDataWriter OpenWriter(ImportContext context)
        {
            return new CsvProductWriter(_extendedProductSearchService, _propertyMetadataLoader, _itemService, _categoryService);
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        public Task<ValidationResult> ValidateAsync(ImportContext context)
        {
            throw new NotImplementedException();
        }
    }
}
