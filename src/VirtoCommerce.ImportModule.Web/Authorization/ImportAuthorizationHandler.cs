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
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                return;
            }

            // Succeeds when the user has the required permission
            await base.HandleRequirementAsync(context, requirement);

            // Organization members are granted access too, but are scoped to their own organization
            var organization = await context.User.ResolveOrganization(_memberResolver);
            if (organization != null)
            {
                if (context.Resource is AuthorizationInfo authorizationInfo)
                {
                    authorizationInfo.OrganizationId = organization.Id;
                }

                context.Succeed(requirement);
            }
        }
    }
}
