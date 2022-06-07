using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ImportModule.Core.Models.Search
{
    public class SearchImportRunHistoryCriteria : SearchCriteriaBase
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ProfileId { get; set; }
        public string JobId { get; set; }
    }
}
