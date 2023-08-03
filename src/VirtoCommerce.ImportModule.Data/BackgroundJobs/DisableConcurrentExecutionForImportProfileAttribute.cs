using System;
using System.Linq;
using System.Security.Cryptography;
using Hangfire.Common;
using Hangfire.Server;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Data.BackgroundJobs
{
    public class DisableConcurrentExecutionForImportProfileAttribute : JobFilterAttribute, IServerFilter
    {
        private readonly int _timeoutInSeconds;

        public DisableConcurrentExecutionForImportProfileAttribute(int timeoutInSeconds)
        {
            if (timeoutInSeconds < 0)
                throw new ArgumentException("Timeout argument value should be greater that zero.");

            _timeoutInSeconds = timeoutInSeconds;
        }

        public void OnPerforming(PerformingContext filterContext)
        {
            var fingerprint = GetFingerprint(filterContext.BackgroundJob.Job);

            var timeout = TimeSpan.FromSeconds(_timeoutInSeconds);

            var distributedLock = filterContext.Connection.AcquireDistributedLock(fingerprint, timeout);
            filterContext.Items["DistributedLock"] = distributedLock;
        }

        public void OnPerformed(PerformedContext filterContext)
        {
            if (!filterContext.Items.ContainsKey("DistributedLock"))
            {
                throw new InvalidOperationException("Can not release a distributed lock: it was not acquired.");
            }

            var distributedLock = (IDisposable)filterContext.Items["DistributedLock"];
            distributedLock.Dispose();
        }

        private static string GetFingerprint(Job job)
        {
            var parameters = string.Empty;
            var importProfile = job.Args.OfType<ImportProfile>().FirstOrDefault();
            if (importProfile == null)
            {
                throw new OperationCanceledException("ImportProfile must presents as argument of job function");
            }
            var payload = $"{job.Type.FullName}.{job.Method.Name}.{importProfile.Id}.{importProfile.DataImporterType}.{importProfile.UserId}";
            var hash = SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
            var fingerprint = Convert.ToBase64String(hash);
            return fingerprint;
        }
    }
}
