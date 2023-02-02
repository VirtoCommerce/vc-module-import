using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IImportReporter : IDisposable, ICloneable
    {
        void SetContext(ImportProfile importProfile);
        Task<string> SaveErrorsAsync(List<ErrorInfo> errorsToSave);
    }
}
