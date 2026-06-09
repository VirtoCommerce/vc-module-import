using System;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.PushNotifications;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IImportRunService
    {
        ImportPushNotification RunImportBackgroundJob(ImportProfile importProfile, ImportPushNotification pushNotification);
        ImportPushNotification RunImportBackgroundJob(ImportProfile importProfile);
        void CancelRunBackgroundJob(ImportCancellationRequest cancellationRequest);

        /// <summary>
        /// Re-queues an interrupted import from its saved cursor.
        /// Default: throws <see cref="NotSupportedException"/>.
        /// Downstream replacements that do not support resume can keep the default; the Resume REST endpoint will fail
        /// at invocation, but existing compilations continue to work.
        /// </summary>
        Task<ImportPushNotification> ResumeImportAsync(string runHistoryId) =>
            throw new NotSupportedException($"{nameof(ResumeImportAsync)} is not implemented by this {nameof(IImportRunService)}.");

        Task<ImportPushNotification> RunImportAsync(ImportProfile importProfile, ImportPushNotification pushNotification, CancellationToken cancellationToken);
        Task<ImportPushNotification> RunImportAsync(ImportProfile importProfile, CancellationToken cancellationToken);
        Task<ImportDataPreview> PreviewAsync(ImportProfile importProfile);
        Task<ValidationResult> ValidateAsync(ImportProfile importProfile);
    }
}
