using System;
using System.Collections.Generic;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IDataImporterRegistrar
    {
        IEnumerable<IDataImporter> AllRegisteredImporters { get; }
        DataImporterBuilder Register<TDataImporter>(Func<IDataImporter> factory = null) where TDataImporter : IDataImporter;
    }
}
