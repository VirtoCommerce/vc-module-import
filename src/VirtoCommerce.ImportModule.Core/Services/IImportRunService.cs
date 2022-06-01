using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.PushNotifications;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IImportRunService
    {
        ImportPushNotification RunImport(ImportProfile importProfile);
        void CancelJob(ImportCancellationRequest cancellationRequest);
        Task<ImportDataPreview> PreviewAsync(ImportProfile importProfile);
    }
}
