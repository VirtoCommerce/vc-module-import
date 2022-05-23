using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.ImportModule.Data.Queries.SearchImportProfiles
{
    public interface ISearchImportProfilesService : ISearchService<SearchImportProfilesQuery, SearchImportProfilesResult, ImportProfile>
    {
    }
}
