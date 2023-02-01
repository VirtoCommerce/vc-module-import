using System;
using System.Collections.Generic;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IImportReporterRegistrar
    {
        IEnumerable<IImportReporter> AllRegisteredReporters { get; }
        ImportReporterBuilder Register<TImportReporter>(Func<IImportReporter> factory = null) where TImportReporter : IImportReporter;
    }
}
