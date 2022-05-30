using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.ImportModule.Core.Domains;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Models.Search;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;

namespace VirtoCommerce.ImportModule.Web.Controllers.Api
{
    [ApiController]
    [Route("api/import")]
    public class ImportController : ControllerBase
    {
        private readonly IDataImporterRegistrar _importersRegistry;
        private readonly IImportRunService _importRunService;
        private readonly IImportProfileService _importProfileService;

        public ImportController(
            IDataImporterRegistrar importersRegistry,
            IImportRunService importRunService,
            IImportProfileService importProfileService
            )
        {
            _importersRegistry = importersRegistry;
            _importRunService = importRunService;
            _importProfileService = importProfileService;
        }

        [HttpPost]
        [Route("run")]
        public ActionResult<ImportPushNotification> RunImport([FromBody] ImportProfile importProfile)
        {
            var result = _importRunService.RunImport(importProfile);

            return Ok(result);
        }

        [HttpPost]
        [Route("task/cancel")]
        public ActionResult CancelJob([FromBody] ImportCancellationRequest cancellationRequest)
        {
            _importRunService.CancelJob(cancellationRequest);

            return Ok();
        }

        [HttpPost]
        [Route("preview")]
        public async Task<ActionResult<ImportDataPreview>> Preview([FromBody] ImportProfile importProfile)
        {
            var result = await _importRunService.PreviewAsync(importProfile);

            return result;
        }

        [HttpGet]
        [Route("importers")]
        public ActionResult<IDataImporter[]> GetImporters()
        {
            return Ok(_importersRegistry.AllRegisteredImporters);
        }

        [HttpGet]
        [Route("profiles/{profileId}")]
        public async Task<ActionResult<ImportProfile>> GetImportProfileById([FromRoute] string profileId)
        {
            var result = await _importProfileService.GetImportProfileByIdAsync(profileId);

            return Ok(result);
        }

        [HttpPost]
        [Route("profiles")]
        public async Task<ActionResult<ImportProfile>> CreateImportProfile([FromBody] ImportProfile importProfile)
        {
            var result = await _importProfileService.CreateImportProfileAsync(importProfile);

            return Ok(result);
        }

        [HttpPut]
        [Route("profiles")]
        public async Task<ActionResult<ImportProfile>> UpdateImportProfile([FromBody] ImportProfile importProfile)
        {
            string profileId = importProfile.Id;
            var result = await _importProfileService.UpdateImportProfileAsync(profileId, importProfile);

            return Ok(result);
        }

        [HttpPost]
        [Route("profiles/search")]
        public async Task<ActionResult<SearchImportProfilesResult>> SearchImportProfiles([FromBody] SearchImportProfilesCriteria criteria)
        {
            var result = await _importProfileService.SearchImportProfilesAsync(criteria);

            return Ok(result);
        }

        [HttpDelete]
        [Route("profiles")]
        public async Task<ActionResult> DeleteProfile([FromQuery] string id)
        {
            await _importProfileService.DeleteProfileAsync(id);

            return Ok();
        }

        [HttpPost]
        [Route("profiles/execution/history/search")]
        public async Task<ActionResult<SearchImportRunHistoryResult>> SearchImportRunHistory([FromBody] SearchImportRunHistoryCriteria criteria)
        {
            var result = await _importRunService.SearchImportRunHistoryAsync(criteria);

            return Ok(result);
        }
    }
}
