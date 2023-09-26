using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<OrganizationInfo>> GetOrganizationInfo(string organizationId)
        {
            var authorizationInfo = new AuthorizationInfo();
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, authorizationInfo, new ImportAuthorizationRequirement(ModuleConstants.Security.Permissions.Access));
            if (!authorizationResult.Succeeded)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(organizationId) && authorizationInfo.IsSeller)
            {
                organizationId = authorizationInfo.SellerId;
            }
            if (!string.IsNullOrEmpty(organizationId))
            {
                var organization = await _memberResolver.ResolveMemberByIdAsync(organizationId);
                if (organization != null)
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
