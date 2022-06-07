using FluentValidation;
using VirtoCommerce.CatalogModule.Core.Model;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class CsvProductValidator : AbstractValidator<CatalogProduct>
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
