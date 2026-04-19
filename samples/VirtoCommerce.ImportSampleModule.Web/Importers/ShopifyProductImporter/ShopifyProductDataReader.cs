using System.IO;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.CsvHelper;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class ShopifyProductDataReader
        : CsvDataReader<ShopifyProductLine, ShopifyProductClassMap>,
          IImportDataReader<ShopifyProductDataCursor>
    {
        private int _rowsRead;

        public ShopifyProductDataReader(Stream stream, ImportContext context)
            : base(stream, context)
        {
        }

        public override async Task<object[]> ReadNextPageAsync(ImportContext context)
        {
            var items = await base.ReadNextPageAsync(context);
            _rowsRead += items.Length;
            return items;
        }

        public ShopifyProductDataCursor GetCursor(ImportContext context)
        {
            return new ShopifyProductDataCursor(_rowsRead);
        }

        public void RestoreCursor(ImportContext context, ShopifyProductDataCursor cursor)
        {
            if (cursor.RowsRead <= 0)
            {
                return;
            }
            if (!CsvReader.Read())
            {
                return;
            }
            CsvReader.ReadHeader();
            CsvReader.ValidateHeader<ShopifyProductLine>();
            while (_rowsRead < cursor.RowsRead && CsvReader.Read())
            {
                _rowsRead++;
            }
        }
    }
}
