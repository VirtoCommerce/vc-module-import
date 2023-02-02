using System;
using System.Collections.Generic;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Core.Models
{
    public class ImportRunHistory : AuditableEntity, ICloneable
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string JobId { get; set; }
        public string ProfileId { get; set; }
        public string ProfileName { get; set; }
        public DateTime Executed { get; set; }
        public DateTime? Finished { get; set; }
        public int TotalCount { get; set; }
        public int ProcessedCount { get; set; }
        public int ErrorsCount { get; set; }
        public ICollection<string> Errors { get; set; }
        public string ReportUrl { get; set; }

        public string TypeName => nameof(ImportRunHistory);

        public ICollection<ObjectSettingEntry> Settings { get; set; }

        public virtual ImportRunHistory CreateNew(ImportProfile profile, ImportPushNotification notification)
        {
            var result = ExType<ImportRunHistory>.New();
            result.UserId = profile.UserId;
            result.UserName = profile.UserName;
            result.ProfileId = profile.Id;
            result.ProfileName = profile.Name;
            result.JobId = notification.JobId;
            result.Executed = notification.Created;
            result.Finished = notification.Finished;
            result.TotalCount = notification.TotalCount;
            result.ProcessedCount = notification.ProcessedCount;
            result.ErrorsCount = notification.ErrorCount;
            result.Errors = notification.Errors;
            result.ReportUrl = notification.ReportUrl;

            return result;
        }

        public object Clone()
        {
            var result = MemberwiseClone() as ImportRunHistory;
            return result;
        }
    }
}
