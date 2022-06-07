using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ImportModule.Core.Models.Search
{
    public class SearchImportProfilesCriteria : SearchCriteriaBase
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
    }
}
