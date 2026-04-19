using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public sealed record ShopifyProductDataCursor(int Row) : ImportDataCursor;
}
