using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.CustomerModule.Core.Extensions;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Authorization;
using VirtoCommerce.Platform.Security.Authorization;

namespace VirtoCommerce.ImportModule.Web.Authorization
{
    public sealed class ImportAuthorizationHandler : PermissionAuthorizationHandlerBase<ImportAuthorizationRequirement>
    {
        public ImportAuthorizationHandler(
            IMemberResolver memberResolver
            )
        {
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ImportAuthorizationRequirement requirement)
        {
            await base.HandleRequirementAsync(context, requirement);

            var organizationId = context.User.GetCurrentOrganizationId();

            if (!string.IsNullOrEmpty(organizationId) && context.Resource != null && context.Resource is AuthorizationInfo authorizationInfo)
            {
                authorizationInfo.OrganizationId = organizationId;
            }

            context.Succeed(requirement);
        }
    }
}
