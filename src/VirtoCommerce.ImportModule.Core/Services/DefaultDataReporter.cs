using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public class DefaultDataReporter : IImportReporter
    {
        public void SetContext(ImportProfile importProfile)
        {
            //do nothing
        }

        public Task<string> SaveErrorsAsync(List<ErrorInfo> errorsToSave)
        {
            return Task.FromResult<string>(null);
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
