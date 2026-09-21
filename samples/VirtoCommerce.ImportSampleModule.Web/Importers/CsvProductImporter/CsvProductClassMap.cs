using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.ImportModule.CsvHelper;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class CsvProductClassMap : ClassMapExtended<CatalogProduct>
    {
        public CsvProductClassMap()
        {
            //you have access to profile's Settings inside class map
            //like Settings.GetSettingValue("your_setting_name", default_value);

            Map(m => m.Name).Optional();
            Map(m => m.Gtin).Optional();
            Map(m => m.CategoryId);
            Map(m => m.OuterId);
            Map(m => m.Images).Optional().TypeConverter<ImagesConverter>();
            Map(m => m.Properties).Index(0).Optional().TypeConverter<PropertiesConverter>();
        }
    }
}
