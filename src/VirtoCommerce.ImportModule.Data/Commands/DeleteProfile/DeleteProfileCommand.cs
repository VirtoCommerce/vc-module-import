using System.ComponentModel.DataAnnotations;
using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;

namespace VirtoCommerce.ImportModule.Data.Commands.DeleteProfile
{
    public class DeleteProfileCommand : ICommand<ImportProfile>, IHasSellerId
    {
        public string SellerId { get; set; }
        public string SellerName { get; set; }

        [Required]
        public string Id { get; set; }
        public ImportProfile ImportProfile { get; set; }
    }
}
