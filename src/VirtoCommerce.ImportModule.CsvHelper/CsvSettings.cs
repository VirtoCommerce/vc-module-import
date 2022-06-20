using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.CsvHelper
{
    public static class CsvSettings
    {
        public static SettingDescriptor Delimiter { get; } = new SettingDescriptor
        {
            Name = "Import.Csv.Delimiter",
            ValueType = SettingValueType.ShortText,
            GroupName = "Import",
            IsDictionary = false,
            DefaultValue = ";"
        };

        public static SettingDescriptor PageSize { get; } = new SettingDescriptor
        {
            Name = "Import.Csv.PageSize",
            ValueType = SettingValueType.PositiveInteger,
            GroupName = "Import",
            IsDictionary = false,
            DefaultValue = "50"
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
