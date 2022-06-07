using System;
using System.Collections.Generic;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using VirtoCommerce.CatalogModule.Core.Model;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class PropertiesConverter : ITypeConverter
    {
        private static IEnumerable<string> ScalarProperties { get; } = typeof(CatalogProduct).GetProperties().Select(x => x.Name);

        public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            var dynamicProperties = new List<Property>();

            var indexCategoryId = Array.IndexOf(row.HeaderRecord, "CategoryId");

            for (int i = 0; i < row.Parser.Count; i++)
            {
                if (!ScalarProperties.Any(str => str == row.HeaderRecord[i]))
                {
                    dynamicProperties.Add(new Property()
                    {
                        Name = row.HeaderRecord[i],
                        CategoryId = row.Parser.Record[indexCategoryId],
                        Type = PropertyType.Product,
                        Values = new List<PropertyValue>()
                        {
                            new PropertyValue()
                            {
                                PropertyName = row.HeaderRecord[i],
                                Value = row.Parser.Record[i]
                            }
                        }
                    });
                }
            }

            return dynamicProperties;
        }

        public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            throw new NotImplementedException();
        }
    }
}
