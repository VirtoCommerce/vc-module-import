using System;

namespace VirtoCommerce.ImportModule.Core.Models
{
    public class ImportContext
    {
        public ImportContext(ImportProfile importProfile)
        {
            ImportProfile = importProfile;
        }

        public ImportProfile ImportProfile { get; private set; }
        public ImportProgressInfo ProgressInfo { get; set; }
        public Action<ErrorInfo> ErrorCallback { get; set; }

        /// <summary>
        /// True when pipeline has successfully restored cursor state from a prior interrupted run.
        /// False on fresh runs or when a cursor was present but invalid/expired.
        /// Observable by importers inside OnImportStartedAsync.
        /// </summary>
        public bool IsResume { get; set; }
    }
}
