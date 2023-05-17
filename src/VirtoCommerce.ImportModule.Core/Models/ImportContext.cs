using System;

namespace VirtoCommerce.ImportModule.Core.Models
{
    public class ImportContext
    {
        public ImportContext(ImportProfile importProfile)
        {
            ImportProfile = importProfile;
            CompleteCallback = importProfile.OnImportCompleted;
        }

        public ImportProfile ImportProfile { get; private set; }
        public ImportProgressInfo ProgressInfo { get; set; }
        public Action<ErrorInfo> ErrorCallback { get; set; }
        public Action CompleteCallback { get; set; }
    }
}
