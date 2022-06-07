using System;
using System.Collections.Generic;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using VirtoCommerce.CatalogModule.Core.Model;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class ImagesConverter : ITypeConverter
    {
        public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            var images = new List<Image>();
            var urls = text.Split(';').ToList();
            var i = 0;
            foreach (var url in urls)
            {
                images.Add(new Image() { Url = url, Group = "images", SortOrder = i });
                i++;
            }

            return images;
        }

        public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            throw new NotImplementedException();
        }
    }
}
