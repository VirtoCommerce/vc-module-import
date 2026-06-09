using System;
using Newtonsoft.Json;

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
        /// True when a pipeline has successfully restored the cursor state from a prior interrupted run.
        /// False on fresh runs or when a cursor was present but invalid/expired.
        /// </summary>
        public bool IsResume { get; set; }

        /// <summary>
        /// JSON settings used by resumable readers to (de)serialize cursors. Null = Newtonsoft defaults.
        /// A reader that needs custom converters or discriminator handling populates this in its
        /// OnImportStartedAsync hook; the resume-cursor DIMs read it on save/restore.
        /// </summary>
        public JsonSerializerSettings JsonSerializerSettings { get; set; }
    }
}
