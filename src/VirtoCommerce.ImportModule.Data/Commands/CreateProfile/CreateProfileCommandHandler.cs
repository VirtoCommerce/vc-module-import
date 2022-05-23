using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.ImportModule.Data.Queries.SearchImportProfiles;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.ImportModule.Data.Commands.CreateProfile
{
    public class CreateProfileCommandHandler : ICommandHandler<CreateProfileCommand, ImportProfile>
    {
        private readonly ICrudService<ImportProfile> _importProfileCrudService;
        private readonly ISearchImportProfilesService _importProfileSearchService;
        public CreateProfileCommandHandler(ICrudService<ImportProfile> importProfileCrudService, ISearchImportProfilesService importProfileSearchService)
        {
            _importProfileCrudService = importProfileCrudService;
            _importProfileSearchService = importProfileSearchService;
        }

        public async Task<ImportProfile> Handle(CreateProfileCommand request, CancellationToken cancellationToken)
        {
            await ExType<CreateProfileValidator>.New().ValidateAndThrowAsync(request);

            var searchResult = await _importProfileSearchService.SearchAsync(new SearchImportProfilesQuery
            {
                SellerId = request.SellerId,
                Name = request.ImportProfile.Name
            });

            if (searchResult.TotalCount == 0)
            {
                var importProfile = ExType<ImportProfile>.New().CreateNew(request.SellerId, request.SellerName,
                request.ImportProfile.Name, request.ImportProfile.DataImporterType, request.ImportProfile.Settings);

                await _importProfileCrudService.SaveChangesAsync(new[] { importProfile });
                return importProfile;
            }
            else
            {
                throw new InvalidOperationException("This profile already exists");
            }
        }
    }
}
