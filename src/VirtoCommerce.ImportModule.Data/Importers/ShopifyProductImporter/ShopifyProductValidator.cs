using FluentValidation;

namespace VirtoCommerce.ImportModule.Data.Importers.ShopifyProductImporter
{
    public class ShopifyProductValidator : AbstractValidator<ShopifyProductLine>
    {
        public ShopifyProductValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithErrorCode("TitleEmpty").WithMessage("Title must be not empty");
            RuleFor(x => x.Handle).NotEmpty().WithErrorCode("HandleIsEmpty").WithMessage("Handle must be not empty");
        }
    }
}
