using FluentValidation;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Data.Validators
{
    public class CreateProfileValidator : AbstractValidator<ImportProfile>
    {
        public CreateProfileValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Settings).NotNull();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.DataImporterType).NotEmpty();
        }
    }
}
