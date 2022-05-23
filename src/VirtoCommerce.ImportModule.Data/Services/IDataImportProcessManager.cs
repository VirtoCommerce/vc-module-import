using System;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.ImportModule.Data.Commands.RunImport;
using VirtoCommerce.ImportModule.Data.Models;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public interface IDataImportProcessManager
    {
        Task ImportAsync(RunImportCommand importDataCommand, Action<ImportProgressInfo> progressCallback, CancellationToken token);
    }
}
