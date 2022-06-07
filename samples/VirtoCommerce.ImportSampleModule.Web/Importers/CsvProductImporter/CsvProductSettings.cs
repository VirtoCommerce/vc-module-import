using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.ImportModule.CsvHelper;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public static class CsvProductSettings
    {

        public static SettingDescriptor CreateDictionaryValues { get; } = new SettingDescriptor
        {
            Name = "Import.Csv.CreateDictionaryValues",
            GroupName = "Import",
            ValueType = SettingValueType.Boolean,
            DefaultValue = false
        };

        public static IEnumerable<SettingDescriptor> AllSettings
        {
            get
            {
                return CsvSettings.AllSettings.Concat(new[] { CreateDictionaryValues });
            }
        }
    }
}
