using System.Linq;
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
            // The permission is always required, organization membership only narrows the data scope
            await base.HandleRequirementAsync(context, requirement);
            if (context.PendingRequirements.Contains(requirement))
            {
                return;
            }

            var organization = await context.User.ResolveOrganization(_memberResolver);
            if (organization != null && context.Resource is AuthorizationInfo authorizationInfo)
            {
                authorizationInfo.OrganizationId = organization.Id;
            }
        }
    }
}
