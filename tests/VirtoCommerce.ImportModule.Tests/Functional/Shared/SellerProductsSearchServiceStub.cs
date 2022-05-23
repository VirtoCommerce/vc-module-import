using System.Threading.Tasks;
using VirtoCommerce.MarketplaceVendorModule.Data.Queries;

namespace VirtoCommerce.MarketplaceVendorModule.Tests.Functional
{
    public class SellerProductsSearchServiceStub : ISellerProductsSearchService
    {
        public Task<SearchProductsResult> SearchAsync(SearchProductsQuery criteria)
        {
            return Task.FromResult(new SearchProductsResult());
        }
    }
}
