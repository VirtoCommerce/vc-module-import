using VirtoCommerce.CatalogModule.Core.Model.Search;

namespace VirtoCommerce.ImportSampleModule.Web.Search
{
    public class ExtendedProductSearchCriteria : ProductSearchCriteria
    {
        public string[] OuterIds { get; set; }
    }
}
