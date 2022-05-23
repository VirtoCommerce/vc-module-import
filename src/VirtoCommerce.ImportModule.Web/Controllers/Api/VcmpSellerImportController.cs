using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.ImportModule.Data.Commands.CancelImport;
using VirtoCommerce.ImportModule.Data.Commands.CreateProfile;
using VirtoCommerce.ImportModule.Data.Commands.DeleteProfile;
using VirtoCommerce.ImportModule.Data.Commands.RunImport;
using VirtoCommerce.ImportModule.Data.Commands.UpdateProfile;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.ImportModule.Data.Queries.GetImportProfile;
using VirtoCommerce.ImportModule.Data.Queries.PreviewData;
using VirtoCommerce.ImportModule.Data.Queries.SearchImportProfiles;
using VirtoCommerce.ImportModule.Data.Queries.SearchImportProfilesHistory;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.MarketplaceVendorModule.Core.PushNotifications;
using VirtoCommerce.MarketplaceVendorModule.Data.Authorization;

namespace VirtoCommerce.MarketplaceVendorModule.Web.Controllers.Api
{
    [ApiController]
    [Route("api/vcmp/import")]
    public class VcmpSellerImportController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAuthorizationService _authorizationService;
        private readonly IDataImporterRegistrar _importersRegistry;

        public VcmpSellerImportController(IMediator mediator, IAuthorizationService authorizationService, IDataImporterRegistrar importersRegistry)
        {
            _mediator = mediator;
            _authorizationService = authorizationService;
            _importersRegistry = importersRegistry;
        }

        [HttpPost]
        [Route("run")]
        public async Task<ActionResult<ImportPushNotification>> RunImport([FromBody] RunImportCommand importCommand)
        {
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, importCommand, new SellerAuthorizationRequirement(Core.ModuleConstants.Security.Permissions.SellerResources));
            if (!authorizationResult.Succeeded)
            {
                return Unauthorized();
            }

            var result = await _mediator.Send(importCommand);

            return Ok(result);
        }

        [HttpPost]
        [Route("task/cancel")]
        [Authorize(Core.ModuleConstants.Security.Permissions.SellerResources)]
        public async Task<ActionResult> CancelJob([FromBody] ImportCancellationRequest cancellationRequest)
        {
            var cancelCommand = new CancelImportCommand { ImportProfileId = cancellationRequest.JobId };

            await _mediator.Send(cancelCommand);

            return Ok();
        }

        [HttpPost]
        [Route("preview")]
        [Authorize(Core.ModuleConstants.Security.Permissions.SellerResources)]
        public async Task<ActionResult<ImportDataPreview>> Preview([FromBody] PreviewDataQuery previewDataQuery)
        {
            var result = await _mediator.Send(previewDataQuery);

            return result;
        }

        [HttpGet]
        [Route("importers")]
        [Authorize(Core.ModuleConstants.Security.Permissions.SellerResources)]
        public ActionResult<IDataImporter[]> GetImporters()
        {
            return Ok(_importersRegistry.AllRegisteredImporters);
        }

        [HttpGet]
        [Route("profiles/{profileId}")]
        public async Task<ActionResult<ImportProfile>> GetImportProfileById([FromRoute] string profileId)
        {
            var query = ExType<GetImportProfileQuery>.New();
            query.ImportProfileId = profileId;
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, query, new SellerAuthorizationRequirement(Core.ModuleConstants.Security.Permissions.SellerResources));
            if (!authorizationResult.Succeeded)
            {
                return Unauthorized();
            }
            var result = await _mediator.Send(query);

            return Ok(result);
        }

        [HttpPost]
        [Route("profiles")]
        public async Task<ActionResult<ImportProfile>> CreateImportProfile([FromBody] CreateProfileCommand command)
        {
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, command, new SellerAuthorizationRequirement(Core.ModuleConstants.Security.Permissions.SellerResources));
            if (!authorizationResult.Succeeded)
            {
                return Unauthorized();
            }
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        [HttpPut]
        [Route("profiles")]
        public async Task<ActionResult<ImportProfile>> UpdateImportProfile([FromBody] UpdateProfileCommand command)
        {
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, command, new SellerAuthorizationRequirement(Core.ModuleConstants.Security.Permissions.SellerResources));
            if (!authorizationResult.Succeeded)
            {
                return Unauthorized();
            }
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        [HttpPost]
        [Route("profiles/search")]
        public async Task<ActionResult<SearchImportProfilesResult>> SearchImportProfiles([FromBody] SearchImportProfilesQuery query)
        {
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, query, new SellerAuthorizationRequirement(Core.ModuleConstants.Security.Permissions.SellerResources));
            if (!authorizationResult.Succeeded)
            {
                return Unauthorized();
            }
            var result = await _mediator.Send(query);

            return Ok(result);
        }

        [HttpDelete]
        [Route("profiles")]
        public async Task<ActionResult> DeleteProfile([FromQuery] string id)
        {
            var command = new DeleteProfileCommand { Id = id };

            var authorizationResult = await _authorizationService.AuthorizeAsync(User, command, new SellerAuthorizationRequirement(Core.ModuleConstants.Security.Permissions.SellerResources));
            if (!authorizationResult.Succeeded)
            {
                return Unauthorized();
            }
            await _mediator.Send(command);

            return Ok();
        }

        [HttpPost]
        [Route("profiles/execution/history/search")]
        public async Task<ActionResult<SearchImportProfilesHistoryResult>> SearchImportProfilesHistory([FromBody] SearchImportProfilesHistoryQuery query)
        {
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, query, new SellerAuthorizationRequirement(Core.ModuleConstants.Security.Permissions.SellerResources));
            if (!authorizationResult.Succeeded)
            {
                return Unauthorized();
            }
            var result = await _mediator.Send(query);

            return Ok(result);
        }
    }
}
