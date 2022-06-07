using CsvHelper.Configuration;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class CsvProductImageClassMap : ClassMap<ProductImage>
    {
        public CsvProductImageClassMap()
        {
            Map(m => m.ProductId);
            Map(m => m.ImageUrl);
        }
    }
}
