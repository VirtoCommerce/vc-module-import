using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.CsvHelper
{
    public static class CsvSettings
    {
        public static SettingDescriptor Delimiter { get; } = new()
        {
            Name = "Import.Csv.Delimiter",
            ValueType = SettingValueType.ShortText,
            GroupName = "Import",
            IsDictionary = false,
            DefaultValue = ";",
            IsRequired = true,
        };

        public static SettingDescriptor PageSize { get; } = new()
        {
            Name = "Import.Csv.PageSize",
            ValueType = SettingValueType.PositiveInteger,
            GroupName = "Import",
            IsDictionary = false,
            DefaultValue = 50,
        };

        public static IEnumerable<SettingDescriptor> AllSettings
        {
            get
            {
                yield return Delimiter;
                yield return PageSize;
            }
        }
    }
}
