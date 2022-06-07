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


        public static IEnumerable<SettingDescriptor> AllSettings
        {
            get
            {
                yield return Delimiter;
            }
        }
    }
}
