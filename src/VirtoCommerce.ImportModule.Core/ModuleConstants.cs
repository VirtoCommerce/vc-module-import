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
    }
}
