using FluentValidation;

namespace VirtoCommerce.ImportModule.Data.Commands.UpdateProfile
{
    public class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
    {
        public UpdateProfileValidator()
        {
            RuleFor(x => x.ImportProfile.DataImporterType).NotEmpty();
        }
    }
}
