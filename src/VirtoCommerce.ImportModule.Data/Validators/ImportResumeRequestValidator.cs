using FluentValidation;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Data.Validators
{
    public class ImportResumeRequestValidator : AbstractValidator<ImportResumeRequest>
    {
        public ImportResumeRequestValidator()
        {
            RuleFor(x => x.JobId).NotEmpty();
        }
    }
}
