using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.MarketplaceVendorModule.Core.PushNotifications;

namespace VirtoCommerce.ImportModule.Data.Commands.RunImport
{
    public class RunImportCommand : ICommand<ImportPushNotification>, IHasSellerId
    {
        public string SellerId { get; set; }
        public string SellerName { get; set; }
        public ImportProfile ImportProfile { get; set; }
    }
}
