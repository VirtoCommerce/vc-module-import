using System;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.ImportModule.Data.Commands.DeleteProfile
{
    public class DeleteProfileCommandHandler : ICommandHandler<DeleteProfileCommand, ImportProfile>
    {
        private readonly ICrudService<ImportProfile> _importProfileCrudService;
        public DeleteProfileCommandHandler(ICrudService<ImportProfile> importProfileCrudService)
        {
            _importProfileCrudService = importProfileCrudService;
        }

        public async Task<ImportProfile> Handle(DeleteProfileCommand request, CancellationToken cancellationToken)
        {
            var importProfile = await _importProfileCrudService.GetByIdAsync(request.Id);

            if (importProfile == null)
            {
                throw new OperationCanceledException($"ImportProfile with {request.Id} is not found");
            }
            await _importProfileCrudService.DeleteAsync(new[] { importProfile.Id });

            return importProfile;
        }
    }
}
