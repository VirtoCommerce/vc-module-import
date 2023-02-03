using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class TestDataReporter : IImportReporter
    {
        public void SetContext(ImportProfile importProfile)
        {
            // do nothing
        }

        public Task<string> SaveErrorsAsync(List<ErrorInfo> errorsToSave)
        {
            return Task.FromResult(nameof(TestDataReporter));
        }

        public void Dispose()
        {
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }
}
