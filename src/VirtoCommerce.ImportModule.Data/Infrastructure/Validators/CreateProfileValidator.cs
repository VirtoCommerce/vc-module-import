using FluentValidation;
using VirtoCommerce.ImportModule.Core.Domains;

namespace VirtoCommerce.ImportModule.Data.Infrastructure.Validators
{
    public class CreateProfileValidator : AbstractValidator<ImportProfile>
    {
        public CreateProfileValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Settings).NotNull();
            RuleFor(x => x.DataImporterType).NotEmpty();
        }
    }
}
