using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Data.Importers
{
    public class TestSettings
    {
        public static SettingDescriptor ShortTextSetting { get; } = new SettingDescriptor
        {
            Name = "Import.Test.ShortTextSetting",
            ValueType = SettingValueType.ShortText,
            GroupName = "Import",
            IsDictionary = false,
            DefaultValue = "ShortText"
        };

        public static SettingDescriptor TotalCount { get; } = new SettingDescriptor
        {
            Name = "Import.Test.TotalCount",
            ValueType = SettingValueType.Integer,
            GroupName = "Import",
            IsDictionary = false,
            DefaultValue = 100
        };

        public static SettingDescriptor IsErrors { get; } = new SettingDescriptor
        {
            Name = "Import.Test.IsErrors",
            ValueType = SettingValueType.Boolean,
            GroupName = "Import",
            IsDictionary = false,
            DefaultValue = false
        };

        public static SettingDescriptor DictionarySetting { get; } = new SettingDescriptor
        {
            Name = "Import.Test.DictionarySetting",
            AllowedValues = new[] { "value a", "value b", " value c" },
            ValueType = SettingValueType.ShortText,
            GroupName = "Import",
            IsDictionary = true,
            DefaultValue = "Dictionary"
        };

        public static IEnumerable<SettingDescriptor> AllSettings
        {
            get
            {
                yield return ShortTextSetting;
                yield return IsErrors;
                yield return DictionarySetting;
                yield return TotalCount;
            }
        }
    }
}
