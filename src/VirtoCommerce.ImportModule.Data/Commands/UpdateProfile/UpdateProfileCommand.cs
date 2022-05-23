using System.ComponentModel.DataAnnotations;
using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;

namespace VirtoCommerce.ImportModule.Data.Commands.UpdateProfile
{
    public class UpdateProfileCommand : ICommand<ImportProfile>, IHasSellerId
    {
        public string SellerId { get; set; }
        public string SellerName { get; set; }
        [Required]
        public string ImportProfileId { get; set; }
        public ImportProfile ImportProfile { get; set; }
    }
}
