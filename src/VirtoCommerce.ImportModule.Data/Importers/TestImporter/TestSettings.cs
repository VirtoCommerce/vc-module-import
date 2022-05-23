using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Data.Importers
{
    public class TestSettings
    {
        public static SettingDescriptor ShortTextSetting { get; } = new SettingDescriptor
        {
            Name = "Vcmp.Import.Test.ShortTextSetting",
            ValueType = SettingValueType.ShortText,
            GroupName = "VCMP|Import",
            IsDictionary = false,
            DefaultValue = "ShortText"
        };

        public static SettingDescriptor TotalCount { get; } = new SettingDescriptor
        {
            Name = "Vcmp.Import.Test.TotalCount",
            ValueType = SettingValueType.Integer,
            GroupName = "VCMP|Import",
            IsDictionary = false,
            DefaultValue = 100
        };

        public static SettingDescriptor IsErrors { get; } = new SettingDescriptor
        {
            Name = "Vcmp.Import.Test.IsErrors",
            ValueType = SettingValueType.Boolean,
            GroupName = "VCMP|Import",
            IsDictionary = false,
            DefaultValue = false
        };

        public static SettingDescriptor DictionarySetting { get; } = new SettingDescriptor
        {
            Name = "Vcmp.Import.Test.DictionarySetting",
            AllowedValues = new[] {"value a", "value b", " value c" },
            ValueType = SettingValueType.ShortText,
            GroupName = "VCMP|Import",
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
