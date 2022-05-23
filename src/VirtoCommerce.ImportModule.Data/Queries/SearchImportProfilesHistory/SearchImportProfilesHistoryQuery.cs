using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ImportModule.Data.Queries.SearchImportProfilesHistory
{
    public class SearchImportProfilesHistoryQuery : SearchCriteriaBase, IQuery<SearchImportProfilesHistoryResult>, IHasSellerId
    {
        public string SellerId { get; set; }
        public string SellerName { get; set; }
        public string ProfileId { get; set; }
        public string JobId { get; set; }
    }
}
