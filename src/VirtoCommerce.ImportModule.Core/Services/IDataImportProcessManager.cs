using System;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IDataImportProcessManager
    {
        Task ImportAsync(ImportProfile importProfile, Func<ImportProgressInfo, Task> progressCallback, CancellationToken token);
    }
}
