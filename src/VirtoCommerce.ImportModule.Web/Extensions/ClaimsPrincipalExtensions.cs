using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;

namespace VirtoCommerce.ImportModule.Web.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static async Task<Member> ResolveOrganization(this ClaimsPrincipal claimsPrincipal, IMemberResolver memberResolver)
        {
            var memberId = claimsPrincipal.GetValue("memberId");
            var member = await memberResolver.ResolveMemberByIdAsync(memberId);
            if (member is Employee employee && employee.Organizations.Any())
            {
                var organization = await memberResolver.ResolveMemberByIdAsync(employee.Organizations.FirstOrDefault());
                return organization;
            }

            return null;
        }


        public static string[] ResolveRoles(this ClaimsPrincipal claimsPrincipal)
        {
            var roles = claimsPrincipal.GetValues("role");

            return roles;
        }

        public static string GetValue(this ClaimsPrincipal principal, string key)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }
            var claim = principal.FindFirst(key);
            return claim?.Value;
        }

        public static string[] GetValues(this ClaimsPrincipal principal, string key)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }
            var claims = principal.FindAll(key);
            return claims?.Select(x => x.Value)?.ToArray();
        }

    }
}
