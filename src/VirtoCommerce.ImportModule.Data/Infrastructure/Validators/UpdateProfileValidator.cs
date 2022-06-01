using FluentValidation;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Data.Infrastructure.Validators
{
    public class UpdateProfileValidator : AbstractValidator<ImportProfile>
    {
        public UpdateProfileValidator()
        {
            RuleFor(x => x.DataImporterType).NotEmpty();
        }
    }
}
