using CsvHelper.Configuration;

namespace VirtoCommerce.ImportModule.Data.Importers
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
