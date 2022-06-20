using FluentValidation;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class CsvProductImageValidator : AbstractValidator<ProductImage>
    {
        public CsvProductImageValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty()
                .WithErrorCode("ProductIdIsEmpty")
                .WithMessage("ProductId must be not empty");

            RuleFor(x => x.ImageUrl).NotEmpty()
                .WithErrorCode("ImageUrlIsEmpty")
                .WithMessage("ImageUrl must be not empty");
        }
    }
}
