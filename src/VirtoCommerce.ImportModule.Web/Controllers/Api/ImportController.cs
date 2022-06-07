using System;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.CatalogModule.Core;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Models.Search;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Validators;

namespace VirtoCommerce.ImportModule.Web.Controllers.Api
{
    [ApiController]
    [Route("api/import")]
    public class ImportController : ControllerBase
    {
        private readonly IDataImporterRegistrar _importersRegistry;
        private readonly IImportRunService _importRunService;
        private readonly IImportProfilesSearchService _importProfilesSearchService;
        private readonly IImportRunHistorySearchService _importRunHistorySearchService;
        private readonly IImportProfileCrudService _importProfileCrudService;

        public ImportController(
            IDataImporterRegistrar importersRegistry,
            IImportRunService importRunService,
            IImportProfilesSearchService importProfilesSearchService,
            IImportRunHistorySearchService importRunHistorySearchService,
            IImportProfileCrudService importProfileCrudService
            )
        {
            _importersRegistry = importersRegistry;
            _importRunService = importRunService;
            _importProfilesSearchService = importProfilesSearchService;
            _importRunHistorySearchService = importRunHistorySearchService;
            _importProfileCrudService = importProfileCrudService;
        }

        [HttpPost]
        [Route("run")]
        [Authorize(ModuleConstants.Security.Permissions.Access)]
        public ActionResult<ImportPushNotification> RunImport([FromBody] ImportProfile importProfile)
        {
            var result = _importRunService.RunImportBackgroundJob(importProfile);

            return Ok(result);
        }

        [HttpPost]
        [Route("task/cancel")]
        [Authorize(ModuleConstants.Security.Permissions.Access)]
        public ActionResult CancelJob([FromBody] ImportCancellationRequest cancellationRequest)
        {
            _importRunService.CancelRunBackgroundJob(cancellationRequest);

            return Ok();
        }

        [HttpPost]
        [Route("preview")]
        [Authorize(ModuleConstants.Security.Permissions.Access)]
        public async Task<ActionResult<ImportDataPreview>> Preview([FromBody] ImportProfile importProfile)
        {
            var result = await _importRunService.PreviewAsync(importProfile);

            return Ok(result);
        }

        [HttpGet]
        [Route("importers")]
        [Authorize(ModuleConstants.Security.Permissions.Access)]
        public ActionResult<IDataImporter[]> GetImporters()
        {
            return Ok(_importersRegistry.AllRegisteredImporters);
        }

        [HttpGet]
        [Route("profiles/{profileId}")]
        [Authorize(ModuleConstants.Security.Permissions.Access)]
        public async Task<ActionResult<ImportProfile>> GetImportProfileById([FromRoute] string profileId)
        {
            var result = await _importProfileCrudService.GetByIdAsync(profileId);

            return Ok(result);
        }

        [HttpPost]
        [Route("profiles")]
        [Authorize(ModuleConstants.Security.Permissions.Create)]
        public async Task<ActionResult<ImportProfile>> CreateImportProfile([FromBody] ImportProfile importProfile)
        {
            await ExType<CreateProfileValidator>.New().ValidateAndThrowAsync(importProfile);

            var searchResult = await _importProfilesSearchService.SearchAsync(new SearchImportProfilesCriteria
            {
                UserId = importProfile.UserId,
                Name = importProfile.Name
            });

            if (searchResult.TotalCount == 0)
            {
                await _importProfileCrudService.SaveChangesAsync(new[] { importProfile });
                return Ok(importProfile);
            }
            else
            {
                throw new InvalidOperationException("This profile already exists");
            }
        }

        [HttpPut]
        [Route("profiles")]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        public async Task<ActionResult<ImportProfile>> UpdateImportProfile([FromBody] ImportProfile importProfile)
        {
            string profileId = importProfile.Id;
            await ExType<UpdateProfileValidator>.New().ValidateAndThrowAsync(importProfile);

            var existedImportProfile = await _importProfileCrudService.GetByIdAsync(profileId);

            if (existedImportProfile == null)
            {
                throw new OperationCanceledException($"ImportProfile with {profileId} is not found");
            }

            existedImportProfile.Update(importProfile);
            await _importProfileCrudService.SaveChangesAsync(new[] { existedImportProfile });

            return Ok(existedImportProfile);
        }

        [HttpPost]
        [Route("profiles/search")]
        [Authorize(ModuleConstants.Security.Permissions.Access)]
        public async Task<ActionResult<SearchImportProfilesResult>> SearchImportProfiles([FromBody] SearchImportProfilesCriteria criteria)
        {
            var result = await _importProfilesSearchService.SearchAsync(criteria);

            return Ok(result);
        }

        [HttpDelete]
        [Route("profiles")]
        [Authorize(ModuleConstants.Security.Permissions.Delete)]
        public async Task<ActionResult> DeleteProfile([FromQuery] string profileId)
        {
            var importProfile = await _importProfileCrudService.GetByIdAsync(profileId);

            if (importProfile == null)
            {
                throw new OperationCanceledException($"ImportProfile with {profileId} is not found");
            }
            await _importProfileCrudService.DeleteAsync(new[] { importProfile.Id });

            return Ok();
        }

        [HttpPost]
        [Route("profiles/execution/history/search")]
        [Authorize(ModuleConstants.Security.Permissions.Access)]
        public async Task<ActionResult<SearchImportRunHistoryResult>> SearchImportRunHistory([FromBody] SearchImportRunHistoryCriteria criteria)
        {
            var result = await _importRunHistorySearchService.SearchAsync(criteria);

            return Ok(result);
        }
    }
}
