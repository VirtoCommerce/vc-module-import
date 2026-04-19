using System.IO;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.CsvHelper;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class ShopifyProductDataReader
        : CsvDataReader<ShopifyProductLine, ShopifyProductClassMap>,
          IImportDataReader<ShopifyProductDataCursor>
    {
        public ShopifyProductDataReader(Stream stream, ImportContext context)
            : base(stream, context)
        {
        }

        public ShopifyProductDataCursor GetCursor(ImportContext context)
        {
            return new ShopifyProductDataCursor(CsvReader.Parser.Row);
        }

        public void RestoreCursor(ImportContext context, ShopifyProductDataCursor cursor)
        {
            if (cursor.Row > 1 && CsvReader.Read())
            {
                CsvReader.ReadHeader();
                CsvReader.ValidateHeader<ShopifyProductLine>();
                while (CsvReader.Read() && CsvReader.Parser.Row < cursor.Row)
                {
                    //Skip to the saved row
                }
            }
        }
    }
}
