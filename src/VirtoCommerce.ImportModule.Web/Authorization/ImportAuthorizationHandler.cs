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
            await base.HandleRequirementAsync(context, requirement);

            var roles = context.User.ResolveRoles();
            if (roles != null && roles.Any())
            {
                if (roles.Contains("Marketplace Operator"))
                {
                    if (context.Resource != null && context.Resource is AuthorizationInfo authorizationInfo)
                    {
                        authorizationInfo.IsOperator = true;
                    }
                    context.Succeed(requirement);
                }
                else if (roles.Contains("Vendor Owner") || roles.Contains("Vendor Admin") || roles.Contains("Vendor Agent"))
                {
                    var organization = await context.User.ResolveOrganization(_memberResolver);

                    if (organization == null)
                    {
                        context.Fail(new AuthorizationFailureReason(this, "Organization doesn't exist"));
                        return;
                    }

                    if (context.Resource != null && context.Resource is AuthorizationInfo authorizationInfo)
                    {
                        authorizationInfo.SellerId = organization.Id;
                        authorizationInfo.IsSeller = true;
                    }
                    context.Succeed(requirement);
                }
            }
            else
            {
                context.Fail(new AuthorizationFailureReason(this, "User has no role"));
            }
        }
    }
}
