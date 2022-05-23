using System;
using System.Collections.Generic;
using MediatR;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains.SellerProductAggregate;
using VirtoCommerce.MarketplaceVendorModule.Data.Queries;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Data.Importers
{
    public class CsvProductImporter : IDataImporter
    {
        private readonly IBlobStorageProvider _blobStorageProvider;
        private readonly IMediator _mediator;
        private readonly ISellerProductsSearchService _sellerProductsSearchService;
        private readonly PropertyMetadataLoader _propertyMetadataLoader;

        public CsvProductImporter(
            IBlobStorageProvider blobStorageProvider,
            IMediator mediator,
            ISellerProductsSearchService sellerProductsSearchService,
            PropertyMetadataLoader propertyMetadataLoader)
        {
            _blobStorageProvider = blobStorageProvider;
            _mediator = mediator;
            _sellerProductsSearchService = sellerProductsSearchService;
            _propertyMetadataLoader = propertyMetadataLoader;
            Metadata = new Dictionary<string, string>()
            {
                { "sampleCsvUrl", "/Modules/$(VirtoCommerce.MarketplaceVendor)/Content/seller_product_import_template.csv" }
            };
        }

        public virtual string TypeName { get; } = nameof(CsvProductImporter);

        public virtual SettingDescriptor[] AvailSettings { get; set; }

        public virtual Dictionary<string, string> Metadata { get; }


        public virtual IImportDataReader OpenReader(ImportContext context)
        {
            if (string.IsNullOrEmpty(context.ImportProfile.ImportFileUrl))
            {
                throw new OperationCanceledException($"Import file must be set");
            }
            var importStream = _blobStorageProvider.OpenRead(context.ImportProfile.ImportFileUrl);

            return new CsvDataReader<ProductDetails, CsvProductClassMap>(importStream, context);
        }

        public virtual IImportDataWriter OpenWriter(ImportContext context)
        {
            return new CsvProductWriter(_mediator, _sellerProductsSearchService, _propertyMetadataLoader);
        }

        public virtual object Clone()
        {
            return base.MemberwiseClone();
        }
    }
}
