using System;
using System.Collections.Generic;
using MediatR;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.MarketplaceVendorModule.Data.Queries;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Data.Importers.ShopifyProductImporter
{
    public class ShopifyProductImporter : IDataImporter
    {
        private readonly IBlobStorageProvider _blobStorageProvider;
        private readonly IMediator _mediator;
        private readonly ISellerProductsSearchService _sellerProductsSearchService;

        public ShopifyProductImporter(IBlobStorageProvider blobStorageProvider,
            IMediator mediator,
            ISellerProductsSearchService sellerProductsSearchService
            )
        {
            _blobStorageProvider = blobStorageProvider;
            _mediator = mediator;
            _sellerProductsSearchService = sellerProductsSearchService;
        }

        public string TypeName { get; } = nameof(ShopifyProductImporter);

        public Dictionary<string, string> Metadata { get; private set; }

        public SettingDescriptor[] AvailSettings { get; set; }

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
            return new ShopifyProductWriter(context, _mediator, _sellerProductsSearchService);
        }

        public object Clone()
        {
            var result = MemberwiseClone() as ShopifyProductImporter;
            return result;
        }
    }
}
