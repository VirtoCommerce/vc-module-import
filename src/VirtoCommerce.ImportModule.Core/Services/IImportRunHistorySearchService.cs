using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Models.Search;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IImportRunHistorySearchService : ISearchService<SearchImportRunHistoryCriteria, SearchImportRunHistoryResult, ImportRunHistory>
    {
    }
}
