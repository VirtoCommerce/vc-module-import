using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.ImportModule.CsvHelper;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class ShopifyProductSettings
    {
        public static SettingDescriptor DebugSetting { get; } = new SettingDescriptor
        {
            Name = "Import.ShopifyProduct.Debug",
            ValueType = SettingValueType.Boolean,
            GroupName = "Import",
            DefaultValue = false
        };

        public static SettingDescriptor CategoryIdSetting { get; } = new SettingDescriptor
        {
            Name = "Import.ShopifyProduct.CategoryId",
            ValueType = SettingValueType.ShortText,
            GroupName = "Import",
            DefaultValue = string.Empty
        };

        public static IEnumerable<SettingDescriptor> AllSettings
        {
            get
            {
                return CsvSettings.AllSettings.Concat(new[] { DebugSetting, CategoryIdSetting });
            }
        }
    }
}
