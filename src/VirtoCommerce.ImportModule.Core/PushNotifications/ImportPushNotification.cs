using System;
using System.Collections.Generic;
using VirtoCommerce.Platform.Core.PushNotifications;

namespace VirtoCommerce.ImportModule.Core.PushNotifications
{
    public class ImportPushNotification : PushNotification
    {
        public ImportPushNotification(string creator)
            : base(creator)
        {
        }
        public string ProfileId { get; set; }
        public string ProfileName { get; set; }

        public string JobId { get; set; }
        public bool EstimatingRemaining { get; set; }
        public TimeSpan? EstimatedRemaining { get; set; }
        public DateTime? Finished { get; set; }
        public int TotalCount { get; set; }
        public int ProcessedCount { get; set; }
        public int ErrorCount => Errors?.Count ?? 0;
        public ICollection<string> Errors { get; set; } = new List<string>();
        public string ReportUrl { get; set; }
    }
}
