using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Authorization;
using VirtoCommerce.ImportModule.Web.Extensions;
using VirtoCommerce.Platform.Security.Authorization;

namespace VirtoCommerce.ImportModule.Web.Authorization
{
    public sealed class ImportAuthorizationHandler : PermissionAuthorizationHandlerBase<ImportAuthorizationRequirement>
    {
        private readonly IMemberResolver _memberResolver;

        public ImportAuthorizationHandler(
            IMemberResolver memberResolver
            )
        {
            _memberResolver = memberResolver;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ImportAuthorizationRequirement requirement)
        {
            // Succeeds for a caller who holds the permission
            await base.HandleRequirementAsync(context, requirement);

            var organization = await context.User.ResolveOrganization(_memberResolver);
            if (organization is null)
            {
                return;
            }

            // An organization's employee is admitted, limited to that organization's profiles and runs
            if (context.Resource is AuthorizationInfo authorizationInfo)
            {
                authorizationInfo.OrganizationId = organization.Id;
            }
            context.Succeed(requirement);
        }
    }
}
