using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Models.Search;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IImportProfilesSearchService : ISearchService<SearchImportProfilesCriteria, SearchImportProfilesResult, ImportProfile>
    {
    }
}
