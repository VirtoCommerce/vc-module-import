using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Domains;
using VirtoCommerce.ImportModule.Core.Models.Search;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IImportProfileService
    {
        Task<ImportProfile> GetImportProfileByIdAsync(string profileId);
        Task<ImportProfile> CreateImportProfileAsync(ImportProfile importProfile);
        Task<ImportProfile> UpdateImportProfileAsync(string profileId, ImportProfile importProfile);
        Task DeleteProfileAsync(string profileId);
        Task<SearchImportProfilesResult> SearchImportProfilesAsync(SearchImportProfilesCriteria criteria);
    }
}
