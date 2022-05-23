using FluentValidation;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains.SellerProductAggregate;

namespace VirtoCommerce.ImportModule.Data.Importers
{
    public class CsvProductValidator : AbstractValidator<ProductDetails>
    {
        public CsvProductValidator()
        {
            //TODO: Add other validation rules
            RuleFor(x => x.CategoryId).NotEmpty()
                .WithErrorCode("CategoryIsEmpty")
                .WithMessage("Category must be not empty");
            RuleFor(x => x.OuterId).NotEmpty()
                .WithErrorCode("OuterIdIsEmpty")
                .WithMessage("OuterId must be not empty");
        }
    }
}
