using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Core
{
    public static class ImportCursorSettings
    {
        private const int DefaultSaveIntervalPages = 5;
        private const int DefaultLifetimeDays = 7;

        public static SettingDescriptor SaveIntervalPages { get; } = new()
        {
            Name = "Import.Cursor.SaveIntervalPages",
            ValueType = SettingValueType.PositiveInteger,
            GroupName = "Import",
            DefaultValue = DefaultSaveIntervalPages,
        };

        public static SettingDescriptor LifetimeDays { get; } = new()
        {
            Name = "Import.Cursor.LifetimeDays",
            ValueType = SettingValueType.PositiveInteger,
            GroupName = "Import",
            DefaultValue = DefaultLifetimeDays,
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
