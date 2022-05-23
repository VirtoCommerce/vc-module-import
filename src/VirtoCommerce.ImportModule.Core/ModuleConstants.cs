using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Core
{
    public static class ModuleConstants
    {
        public static class Security
        {
            public static class Permissions
            {
                public const string Access = "Import:access";
                public const string Create = "Import:create";
                public const string Read = "Import:read";
                public const string Update = "Import:update";
                public const string Delete = "Import:delete";

                public static string[] AllPermissions { get; } = { Read, Create, Access, Update, Delete };
            }
        }

        public static class Settings
        {
            public static class General
            {
                public static SettingDescriptor ImportEnabled { get; } = new SettingDescriptor
                {
                    Name = "Import.ImportEnabled",
                    GroupName = "Import|General",
                    ValueType = SettingValueType.Boolean,
                    DefaultValue = false
                };

                public static SettingDescriptor ImportPassword { get; } = new SettingDescriptor
                {
                    Name = "Import.ImportPassword",
                    GroupName = "Import|Advanced",
                    ValueType = SettingValueType.SecureString,
                    DefaultValue = "qwerty"
                };

                public static IEnumerable<SettingDescriptor> AllSettings
                {
                    get
                    {
                        yield return ImportEnabled;
                        yield return ImportPassword;
                    }
                }
            }

            public static IEnumerable<SettingDescriptor> AllSettings
            {
                get
                {
                    return General.AllSettings;
                }
            }
        }
    }
}
