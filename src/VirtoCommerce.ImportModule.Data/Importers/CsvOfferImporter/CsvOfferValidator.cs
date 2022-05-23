using FluentValidation;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;

namespace VirtoCommerce.ImportModule.Data.Importers
{
    public class CsvOfferValidator : AbstractValidator<OfferDetails>
    {
        public CsvOfferValidator()
        {
            RuleFor(x => x.OuterId).NotEmpty()
                .WithErrorCode("OuterIdIsEmpty")
                .WithMessage("OuterId must be not empty");
        }
    }
}
