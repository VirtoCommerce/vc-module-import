using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Model.Search;

namespace VirtoCommerce.ImportSampleModule.Web.Search
{
    public interface IExtendedProductSearchService
    {
        Task<ProductSearchResult> SearchAsync(ExtendedProductSearchCriteria criteria);
    }
}
