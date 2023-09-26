using VirtoCommerce.Platform.Security.Authorization;

namespace VirtoCommerce.ImportModule.Data.Authorization
{
    public sealed class ImportAuthorizationRequirement : PermissionAuthorizationRequirement
    {
        public ImportAuthorizationRequirement(string permission)
            : base(permission)
        {
        }
    }
}
