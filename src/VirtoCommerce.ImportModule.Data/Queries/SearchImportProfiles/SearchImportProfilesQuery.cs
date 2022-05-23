using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ImportModule.Data.Queries.SearchImportProfiles
{
    public class SearchImportProfilesQuery : SearchCriteriaBase, IQuery<SearchImportProfilesResult>, IHasSellerId
    {
        public string SellerId { get; set; }
        public string SellerName { get; set; }
        public string Name { get; set; }
    }
}
