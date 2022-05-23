using System;
using System.Collections.Generic;
using MediatR;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.MarketplaceVendorModule.Data.Queries;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Data.Importers
{
    public class CsvOfferImporter : IDataImporter
    {
        private readonly IBlobStorageProvider _blobStorageProvider;
        private readonly IMediator _mediator;
        private readonly IOffersSearchService _offersSearchService;

        public CsvOfferImporter(IBlobStorageProvider blobStorageProvider, IMediator mediator, IOffersSearchService offersSearchService)
        {
            _blobStorageProvider = blobStorageProvider;
            _mediator = mediator;
            _offersSearchService = offersSearchService;
            Metadata = new Dictionary<string, string>()
            {
                { "sampleCsvUrl", "/Modules/$(VirtoCommerce.MarketplaceVendor)/Content/seller_offer_import_template.csv" }
            };
        }

        public virtual string TypeName { get; } = nameof(CsvOfferImporter);
        public virtual SettingDescriptor[] AvailSettings { get; set; }

        public virtual Dictionary<string, string> Metadata { get; }

        public IImportDataReader OpenReader(ImportContext context)
        {
            if (string.IsNullOrEmpty(context.ImportProfile.ImportFileUrl))
            {
                throw new OperationCanceledException($"Import file must be set");
            }
            var importStream = _blobStorageProvider.OpenRead(context.ImportProfile.ImportFileUrl);

            return new CsvDataReader<OfferDetails, CsvOfferClassMap>(importStream, context);
        }

        public IImportDataWriter OpenWriter(ImportContext context)
        {
            return new CsvOfferWriter(_mediator, _offersSearchService);
        }

        public object Clone()
        {
            return base.MemberwiseClone();
        }
    }
}
