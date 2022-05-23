using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.ImportModule.Data.Models;

namespace VirtoCommerce.ImportModule.Data.Queries.PreviewData
{
    public class PreviewDataQuery : IQuery<ImportDataPreview>
    {
        public ImportProfile ImportProfile { get; set; }
    }
}
