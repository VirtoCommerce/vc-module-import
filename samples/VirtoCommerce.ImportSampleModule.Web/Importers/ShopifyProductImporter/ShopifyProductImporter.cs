using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.CsvHelper;
using VirtoCommerce.ImportSampleModule.Web.Search;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class ShopifyProductImporter : IDataImporter
    {
        private readonly IBlobStorageProvider _blobStorageProvider;
        private readonly IExtendedProductSearchService _productsSearchService;
        private readonly IItemService _itemService;

        public ShopifyProductImporter(IBlobStorageProvider blobStorageProvider,
            IExtendedProductSearchService productsSearchService,
            IItemService itemService
            )
        {
            _blobStorageProvider = blobStorageProvider;
            _productsSearchService = productsSearchService;
            _itemService = itemService;
        }

        public string TypeName { get; } = nameof(ShopifyProductImporter);

        public Dictionary<string, string> Metadata { get; private set; }

        public SettingDescriptor[] AvailSettings { get; set; }
        public IAuthorizationRequirement AuthorizationReqirement { get; set; }

        public IImportDataReader OpenReader(ImportContext context)
        {
            if (string.IsNullOrEmpty(context.ImportProfile.ImportFileUrl))
            {
                throw new OperationCanceledException($"Import file must be set");
            }
            var importStream = _blobStorageProvider.OpenRead(context.ImportProfile.ImportFileUrl);

            return new CsvDataReader<ShopifyProductLine, ShopifyProductClassMap>(importStream, context);
        }

        public IImportDataWriter OpenWriter(ImportContext context)
        {
            return new ShopifyProductWriter(context, _productsSearchService, _itemService);
        }

        public object Clone()
        {
            var result = MemberwiseClone() as ShopifyProductImporter;
            return result;
        }
    }
}
