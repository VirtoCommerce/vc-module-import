using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.ImportModule.Data.Commands.UpdateProfile
{
    public class UpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand, ImportProfile>
    {
        private readonly ICrudService<ImportProfile> _importProfileCrudService;
        public UpdateProfileCommandHandler(ICrudService<ImportProfile> importProfileCrudService)
        {
            _importProfileCrudService = importProfileCrudService;
        }

        public async Task<ImportProfile> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        {
            await ExType<UpdateProfileValidator>.New().ValidateAndThrowAsync(request);

            var importProfile = await _importProfileCrudService.GetByIdAsync(request.ImportProfileId);

            if (importProfile == null)
            {
                throw new OperationCanceledException($"ImportProfile with {request.ImportProfileId} is not found");
            }

            importProfile.Update(request.ImportProfile);
            await _importProfileCrudService.SaveChangesAsync(new[] { importProfile });

            return importProfile;
        }
    }
}
