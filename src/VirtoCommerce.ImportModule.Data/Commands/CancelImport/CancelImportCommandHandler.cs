using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using MediatR;
using VirtoCommerce.MarketplaceVendorModule.Core.Common;

namespace VirtoCommerce.ImportModule.Data.Commands.CancelImport
{
    public class CancelImportCommandHandler : ICommandHandler<CancelImportCommand>
    {
        public CancelImportCommandHandler()
        {
        }

        public Task<Unit> Handle(CancelImportCommand request, CancellationToken cancellationToken)
        {
            BackgroundJob.Delete(request.ImportProfileId);
            return Task.FromResult(Unit.Value);
        }
    }
}
