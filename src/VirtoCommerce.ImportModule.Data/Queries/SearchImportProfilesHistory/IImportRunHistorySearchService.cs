using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.ImportModule.Data.Queries.SearchImportProfilesHistory
{
    public interface IImportRunHistorySearchService : ISearchService<SearchImportProfilesHistoryQuery, SearchImportProfilesHistoryResult, ImportRunHistory>
    {
    }
}
