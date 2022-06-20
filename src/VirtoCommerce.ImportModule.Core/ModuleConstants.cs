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
                public const string Access = "import:access";
                public const string Create = "import:create";
                public const string Read = "import:read";
                public const string Update = "import:update";
                public const string Delete = "import:delete";
                public const string Execute = "import:execute";

                public static string[] AllPermissions { get; } = { Read, Create, Access, Update, Delete, Execute };
            }
        }

        public static class Settings
        {
            public static class General
            {
                public static SettingDescriptor MaxErrorsCountThreshold { get; } = new SettingDescriptor
                {
                    Name = "Import.MaxErrorsCountThreshold",
                    ValueType = SettingValueType.PositiveInteger,
                    GroupName = "Import",
                    IsDictionary = false,
                    DefaultValue = "50"
                };

                public static IEnumerable<SettingDescriptor> AllSettings
                {
                    get
                    {
                        yield return MaxErrorsCountThreshold;
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
