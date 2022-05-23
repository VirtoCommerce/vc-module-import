using VirtoCommerce.MarketplaceVendorModule.Core.Common;

namespace VirtoCommerce.ImportModule.Data.Commands.CancelImport
{
    public class CancelImportCommand : ICommand, IHasSellerId
    {
        public string SellerId { get; set; }
        public string SellerName { get; set; }
        public string ImportProfileId { get; set; }
    }
}
