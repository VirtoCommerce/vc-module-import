using System;
using System.Threading.Tasks;
using FluentValidation;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Core.Domains;
using VirtoCommerce.ImportModule.Core.Models.Search;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Infrastructure.Validators;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public class ImportProfileService : IImportProfileService
    {
        private readonly ICrudService<ImportProfile> _crudService;
        private readonly IImportProfilesSearchService _importProfilesSearchService;

        public ImportProfileService(ICrudService<ImportProfile> crudService, IImportProfilesSearchService importProfilesSearchService)
        {
            _crudService = crudService;
            _importProfilesSearchService = importProfilesSearchService;
        }

        public async Task<ImportProfile> GetImportProfileByIdAsync(string profileId)
        {
            var result = await _crudService.GetByIdAsync(profileId);
            return result;
        }

        public async Task<ImportProfile> CreateImportProfileAsync(ImportProfile importProfile)
        {
            await ExType<CreateProfileValidator>.New().ValidateAndThrowAsync(importProfile);

            var searchResult = await _importProfilesSearchService.SearchAsync(new SearchImportProfilesCriteria
            {
                SellerId = importProfile.SellerId,
                Name = importProfile.Name
            });

            if (searchResult.TotalCount == 0)
            {
                await _crudService.SaveChangesAsync(new[] { importProfile });
                return importProfile;
            }
            else
            {
                throw new InvalidOperationException("This profile already exists");
            }
        }

        public async Task<ImportProfile> UpdateImportProfileAsync(string profileId, ImportProfile importProfile)
        {
            await ExType<UpdateProfileValidator>.New().ValidateAndThrowAsync(importProfile);

            var existedImportProfile = await _crudService.GetByIdAsync(profileId);

            if (existedImportProfile == null)
            {
                throw new OperationCanceledException($"ImportProfile with {profileId} is not found");
            }

            existedImportProfile.Update(importProfile);
            await _crudService.SaveChangesAsync(new[] { existedImportProfile });

            return existedImportProfile;
        }

        public async Task DeleteProfileAsync(string profileId)
        {
            var importProfile = await _crudService.GetByIdAsync(profileId);

            if (importProfile == null)
            {
                throw new OperationCanceledException($"ImportProfile with {profileId} is not found");
            }
            await _crudService.DeleteAsync(new[] { importProfile.Id });
        }

        public async Task<SearchImportProfilesResult> SearchImportProfilesAsync(SearchImportProfilesCriteria criteria)
        {
            var result = await _importProfilesSearchService.SearchAsync(criteria);
            return result;
        }
    }
}
