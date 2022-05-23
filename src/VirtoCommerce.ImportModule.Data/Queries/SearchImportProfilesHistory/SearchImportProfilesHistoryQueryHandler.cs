using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.MarketplaceVendorModule.Core.Common;

namespace VirtoCommerce.ImportModule.Data.Queries.SearchImportProfilesHistory
{
    public class SearchImportProfilesHistoryQueryHandler : IQueryHandler<SearchImportProfilesHistoryQuery, SearchImportProfilesHistoryResult>
    {
        private readonly IImportRunHistorySearchService _searchService;
        public SearchImportProfilesHistoryQueryHandler(IImportRunHistorySearchService searchService)
        {
            _searchService = searchService;
        }
        public async Task<SearchImportProfilesHistoryResult> Handle(SearchImportProfilesHistoryQuery request, CancellationToken cancellationToken)
        {
            var result = await _searchService.SearchAsync(request);
            return result;
        }
    }
}
