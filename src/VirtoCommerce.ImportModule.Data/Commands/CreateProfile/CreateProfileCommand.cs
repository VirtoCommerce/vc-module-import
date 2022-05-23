using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;

namespace VirtoCommerce.ImportModule.Data.Commands.CreateProfile
{
    public class CreateProfileCommand : ICommand<ImportProfile>, IHasSellerId
    {
        public string SellerId { get; set; }
        public string SellerName { get; set; }
        public ImportProfile ImportProfile { get; set; }
    }
}
