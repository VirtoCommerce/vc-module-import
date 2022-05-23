using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Data.Importers
{
    public class ProductImageImporterSettings
    {
        public static SettingDescriptor DebugSetting { get; } = new SettingDescriptor
        {
            Name = "Vcmp.Import.ProductImage.Debug",
            ValueType = SettingValueType.Boolean,
            GroupName = "Import",
            DefaultValue = false
        };

        public static IEnumerable<SettingDescriptor> AllSettings
        {
            get
            {
                return CsvSettings.AllSettings.Concat(new [] { DebugSetting });
            }
        }
    }
}
