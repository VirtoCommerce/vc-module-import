using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Data.Importers.ShopifyProductImporter
{
    public class ShopifyProductSettings
    {
        public static SettingDescriptor DebugSetting { get; } = new SettingDescriptor
        {
            Name = "Vcmp.Import.ShopifyProduct.Debug",
            ValueType = SettingValueType.Boolean,
            GroupName = "VCMP|Import",
            DefaultValue = false
        };

        public static SettingDescriptor CategoryIdSetting { get; } = new SettingDescriptor
        {
            Name = "Vcmp.Import.ShopifyProduct.CategoryId",
            ValueType = SettingValueType.ShortText,
            GroupName = "VCMP|Import",
            DefaultValue = string.Empty
        };

        public static SettingDescriptor CurrencySetting { get; } = new SettingDescriptor
        {
            Name = "Vcmp.Import.ShopifyProduct.Currency",
            ValueType = SettingValueType.ShortText,
            GroupName = "VCMP|Import",
            DefaultValue = "USD"
        };

        public static IEnumerable<SettingDescriptor> AllSettings
        {
            get
            {
                return CsvSettings.AllSettings.Concat(new[] { DebugSetting, CategoryIdSetting, CurrencySetting });
            }
        }
    }
}
