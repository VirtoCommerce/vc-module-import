using FluentValidation;

namespace VirtoCommerce.ImportModule.Data.Commands.CreateProfile
{
    public class CreateProfileValidator : AbstractValidator<CreateProfileCommand>
    {
        public CreateProfileValidator()
        {
            RuleFor(x => x.ImportProfile.Name).NotEmpty();
            RuleFor(x => x.ImportProfile.Settings).NotNull();
            RuleFor(x => x.ImportProfile.DataImporterType).NotEmpty();
        }
    }
}
