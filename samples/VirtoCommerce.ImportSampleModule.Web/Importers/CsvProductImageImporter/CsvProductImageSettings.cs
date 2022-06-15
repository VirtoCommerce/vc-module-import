using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.ImportModule.CsvHelper;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class CsvProductImageSettings
    {
        public static SettingDescriptor DebugSetting { get; } = new SettingDescriptor
        {
            Name = "Import.ProductImage.Debug",
            ValueType = SettingValueType.Boolean,
            GroupName = "Import",
            DefaultValue = false
        };

        public static IEnumerable<SettingDescriptor> AllSettings
        {
            get
            {
                return CsvSettings.AllSettings.Concat(new[] { DebugSetting });
            }
        }
    }
}
