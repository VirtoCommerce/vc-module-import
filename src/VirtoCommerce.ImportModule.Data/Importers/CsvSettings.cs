using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Data.Importers
{
    public static class CsvSettings
    {
        public static SettingDescriptor Delimiter { get; } = new SettingDescriptor
        {
            Name = "Vcmp.Import.Csv.Delimiter",
            ValueType = SettingValueType.ShortText,
            GroupName = "VCMP|Import",
            IsDictionary = false,
            DefaultValue = ";"
        };


        public static IEnumerable<SettingDescriptor> AllSettings
        {
            get
            {
                yield return Delimiter;
            }
        }
    }
}
