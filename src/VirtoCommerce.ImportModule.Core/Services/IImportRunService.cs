using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.PushNotifications;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IImportRunService
    {
        ImportPushNotification RunImportBackgroundJob(ImportProfile importProfile);
        void CancelRunBackgroundJob(ImportCancellationRequest cancellationRequest);
        Task<ImportPushNotification> RunImportAsync(ImportProfile importProfile, CancellationToken cancellationToken);
        Task<ImportDataPreview> PreviewAsync(ImportProfile importProfile);
        Task<ValidationResult> ValidateAsync(ImportProfile importProfile);
    }
}
