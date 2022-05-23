using System.Globalization;
using CsvHelper.Configuration;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains.SellerProductAggregate;

namespace VirtoCommerce.ImportModule.Data.Importers
{
    public class CsvProductClassMap : ClassMap<ProductDetails>
    {
        public CsvProductClassMap()
        {
            Map(m => m.Name).Optional();
            Map(m => m.Description).Optional();
            Map(m => m.Gtin).Optional();
            Map(m => m.CategoryId);
            Map(m => m.OuterId);
            Map(m => m.Images).Optional().TypeConverter<ImagesConverter>();
            Map(m => m.Properties).Index(0).Optional().TypeConverter<PropertiesConverter>();
        }
    }
}
