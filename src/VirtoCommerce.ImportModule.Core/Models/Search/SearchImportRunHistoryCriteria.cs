using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ImportModule.Core.Models.Search
{
    public class SearchImportRunHistoryCriteria : SearchCriteriaBase
    {
        public string SellerId { get; set; }
        public string SellerName { get; set; }
        public string ProfileId { get; set; }
        public string JobId { get; set; }
    }
}
