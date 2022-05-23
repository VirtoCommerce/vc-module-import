using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.MarketplaceVendorModule.Core.PushNotifications;
using VirtoCommerce.MarketplaceVendorModule.Data.BackgroundJobs;
using VirtoCommerce.ImportModule.Data.BackgroundJobs;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ImportModule.Data.Commands.RunImport
{
    public class RunImportCommandHandler : ICommandHandler<RunImportCommand, ImportPushNotification>
    {
        private readonly IUserNameResolver _userNameResolver;
        private readonly IBackgroundJobExecutor _backgroundJobExecutor;

        public RunImportCommandHandler(IUserNameResolver userNameResolver, IBackgroundJobExecutor backgroundJobExecutor)
        {
            _userNameResolver = userNameResolver;
            _backgroundJobExecutor = backgroundJobExecutor;
        }

        public Task<ImportPushNotification> Handle(RunImportCommand request, CancellationToken cancellationToken)
        {
            var pushNotification = new ImportPushNotification(_userNameResolver.GetCurrentUserName());
            pushNotification.ProfileId = request.ImportProfile.Id;
            pushNotification.ProfileName = request.ImportProfile.Name;

            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var processingSellerJob = monitoringApi.ProcessingJobs(0, int.MaxValue).Select(x => x.Value.Job)
               .Where(x => x.Type == typeof(ImportJob))
               .Select(x => x.Args.OfType<RunImportCommand>())
               .Select(x => x.Any(y => y.SellerId == request.SellerId))
               .FirstOrDefault();

            if (processingSellerJob)
            {
                throw new OperationCanceledException("Concurrent execution is limited");
            }
            var jobId = _backgroundJobExecutor.Enqueue<ImportJob>(x => x.ImportBackgroundAsync(request, pushNotification, JobCancellationToken.Null, null));

            pushNotification.JobId = jobId;

            return Task.FromResult(pushNotification);
        }
    }
}
