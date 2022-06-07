using CsvHelper.Configuration;
using VirtoCommerce.CatalogModule.Core.Model;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class CsvProductClassMap : ClassMap<CatalogProduct>
    {
        public CsvProductClassMap()
        {
            Map(m => m.Name).Optional();
            Map(m => m.Gtin).Optional();
            Map(m => m.CategoryId);
            Map(m => m.OuterId);
            Map(m => m.Images).Optional().TypeConverter<ImagesConverter>();
            Map(m => m.Properties).Index(0).Optional().TypeConverter<PropertiesConverter>();
        }
    }
}
