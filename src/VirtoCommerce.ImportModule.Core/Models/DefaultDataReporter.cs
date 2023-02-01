using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Services;

namespace VirtoCommerce.ImportModule.Core.Models
{
    public class DefaultDataReporter : IImportReporter
    {
        public void SetContext(ImportProfile importProfile)
        {
            //do nothing
        }

        public Task<string> SaveErrorsAsync(List<ErrorInfo> errorsToSave)
        {
            return Task.FromResult(nameof(DefaultDataReporter));
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public void Dispose()
        {
        }
    }
}
