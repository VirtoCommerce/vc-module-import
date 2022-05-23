using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Data.Importers
{
    public static class CsvProductSettings
    {
      
        public static SettingDescriptor CreateDictionaryValues { get; } = new SettingDescriptor
        {
            Name = "Vcmp.Import.Csv.CreateDictionaryValues",
            GroupName = "VCMP|Import",
            ValueType = SettingValueType.Boolean,
            DefaultValue = false
        };

        public static IEnumerable<SettingDescriptor> AllSettings
        {
            get
            {
                return CsvSettings.AllSettings.Concat(new [] { CreateDictionaryValues  });
            }
        }
    }
}
