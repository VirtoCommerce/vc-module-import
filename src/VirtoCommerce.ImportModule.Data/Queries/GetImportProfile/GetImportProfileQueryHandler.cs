using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.ImportModule.Data.Queries.GetImportProfile
{
    public class GetImportProfileQueryHandler : IQueryHandler<GetImportProfileQuery, ImportProfile>
    {
        private readonly ICrudService<ImportProfile> _crudService;
        public GetImportProfileQueryHandler(ICrudService<ImportProfile> crudService)
        {
            _crudService = crudService;
        }

        public async Task<ImportProfile> Handle(GetImportProfileQuery request, CancellationToken cancellationToken)
        {
            var result = await _crudService.GetByIdAsync(request.ImportProfileId);
            return result;
        }
    }
}
