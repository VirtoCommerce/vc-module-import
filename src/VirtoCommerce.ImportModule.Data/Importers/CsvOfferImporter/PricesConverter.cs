using System;
using System.Collections.Generic;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;

namespace VirtoCommerce.ImportModule.Data.Importers
{
    public class PricesConverter : ITypeConverter
    {
        public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            var prices = new List<OfferPrice>();
            for (int i = 0; i < row.Parser.Count; i++)
            {
                var header = row.HeaderRecord[i];
                var priceHeaders = new Dictionary<string, Action<int, string>> {
                    { "ListPrices", (index, value) =>  prices.ElementAtOrAddNew(index).ListPrice = Convert.ToDecimal(value)  },
                    { "SalePrices", (index, value) => prices.ElementAtOrAddNew(index).SalePrice = string.IsNullOrEmpty(value) ? (decimal?)null : Convert.ToDecimal(value) },
                    { "MinQuantity", (index, value) => prices.ElementAtOrAddNew(index).MinQuantity = Convert.ToInt32(value) }
                };
                if (priceHeaders.ContainsKey(header))
                {
                    int index = 0;
                    var values = row.Parser.Record[i].Split(';');
                    foreach (var value in values)
                    {
                        priceHeaders[header](index++, value);
                    }
                }
            }
            return prices;
        }

        public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            throw new NotImplementedException();
        }
    }
}
