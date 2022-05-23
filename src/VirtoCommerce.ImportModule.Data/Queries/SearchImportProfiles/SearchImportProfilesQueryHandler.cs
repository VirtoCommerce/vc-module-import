using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.MarketplaceVendorModule.Core.Common;

namespace VirtoCommerce.ImportModule.Data.Queries.SearchImportProfiles
{
    public class SearchImportProfilesQueryHandler : IQueryHandler<SearchImportProfilesQuery, SearchImportProfilesResult>
    {
        private readonly ISearchImportProfilesService _searchService;
        public SearchImportProfilesQueryHandler(ISearchImportProfilesService searchService)
        {
            _searchService = searchService;
        }
        public async Task<SearchImportProfilesResult> Handle(SearchImportProfilesQuery request, CancellationToken cancellationToken)
        {
            var result = await _searchService.SearchAsync(request);
            return result;
        }
    }
}
