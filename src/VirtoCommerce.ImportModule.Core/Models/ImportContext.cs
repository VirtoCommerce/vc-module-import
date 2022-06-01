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
    }
}
