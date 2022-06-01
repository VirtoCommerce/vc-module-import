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
    }
}
