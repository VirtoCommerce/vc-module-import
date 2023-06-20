using System;
using System.Threading.Tasks;

namespace VirtoCommerce.ImportModule.Core.Models
{
    public class ImportContext
    {
        public ImportContext(ImportProfile importProfile)
        {
            ImportProfile = importProfile;
            CompleteCallback = importProfile.OnImportCompletedAsync;
        }

        public ImportProfile ImportProfile { get; private set; }
        public ImportProgressInfo ProgressInfo { get; set; }
        public Action<ErrorInfo> ErrorCallback { get; set; }
        public Func<Task> CompleteCallback { get; set; }
    }
}
