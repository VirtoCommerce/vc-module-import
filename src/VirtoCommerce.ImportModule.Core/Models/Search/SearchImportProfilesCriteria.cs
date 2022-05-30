using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ImportModule.Core.Models.Search
{
    public class SearchImportProfilesCriteria : SearchCriteriaBase
    {
        public string SellerId { get; set; }
        public string SellerName { get; set; }
        public string Name { get; set; }
    }
}
