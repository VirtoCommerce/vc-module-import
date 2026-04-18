using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Core
{
    public static class ImportCursorSettings
    {
        public static SettingDescriptor SaveIntervalPages { get; } = new()
        {
            Name = "Import.Cursor.SaveIntervalPages",
            ValueType = SettingValueType.PositiveInteger,
            GroupName = "Import",
            DefaultValue = 5,
        };

        public static SettingDescriptor LifetimeDays { get; } = new()
        {
            Name = "Import.Cursor.LifetimeDays",
            ValueType = SettingValueType.PositiveInteger,
            GroupName = "Import",
            DefaultValue = 7,
        };

        public static IEnumerable<SettingDescriptor> AllSettings
        {
            get
            {
                yield return SaveIntervalPages;
                yield return LifetimeDays;
            }
        }
    }
}
