using CsvHelper.Configuration;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;

namespace VirtoCommerce.ImportModule.Data.Importers
{
    public class CsvOfferClassMap : ClassMap<OfferDetails>
    {
        public CsvOfferClassMap()
        {
            Map(m => m.OuterId);
            Map(m => m.ProductId);
            Map(m => m.Sku);
            Map(m => m.Currency);
            Map(m => m.Prices).Index(4).Optional().TypeConverter<PricesConverter>();
        }
    }
}
