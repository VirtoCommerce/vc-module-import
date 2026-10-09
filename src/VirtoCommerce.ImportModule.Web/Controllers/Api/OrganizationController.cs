using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Data.Authorization;
using VirtoCommerce.ImportModule.Web.Authorization;

namespace VirtoCommerce.ImportModule.Web.Controllers.Api
{
    [ApiController]
    [Route("api/import")]
    public class OrganizationController : ControllerBase
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IMemberResolver _memberResolver;

        public OrganizationController(
            IAuthorizationService authorizationService,
            IMemberResolver memberResolver
            )
        {
            _authorizationService = authorizationService;
            _memberResolver = memberResolver;
        }

        [HttpGet]
        [Route("organization")]
        [Authorize]
        public async Task<ActionResult<OrganizationInfo>> GetOrganizationInfo(string organizationId)
        {
            var authorizationInfo = new AuthorizationInfo();
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, authorizationInfo, new ImportAuthorizationRequirement(ModuleConstants.Security.Permissions.Access));
            if (!authorizationResult.Succeeded)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(organizationId))
            {
                organizationId = authorizationInfo.OrganizationId;
            }
            else if (!string.IsNullOrEmpty(authorizationInfo.OrganizationId) && organizationId != authorizationInfo.OrganizationId)
            {
                // An organization's employee reads only its own organization
                return Unauthorized();
            }

            if (!string.IsNullOrEmpty(organizationId))
            {
                var member = await _memberResolver.ResolveMemberByIdAsync(organizationId);
                if (member is Organization organization)
                {
                    var organizationInfo = new OrganizationInfo
                    {
                        OrganizationId = organization.Id,
                        OrganizationName = organization.Name,
                        OrganizationLogoUrl = organization.IconUrl
                    };

                    return Ok(organizationInfo);
                }
            }
            return Ok();
        }
    }
}
