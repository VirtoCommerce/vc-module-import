using System;
using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;

namespace VirtoCommerce.ImportModule.Data.Models
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
