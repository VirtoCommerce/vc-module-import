using System;
using System.Collections.Generic;
using VirtoCommerce.ImportModule.Data.Models;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public interface IDataImporterRegistrar
    {
        IEnumerable<IDataImporter> AllRegisteredImporters { get; }
        DataImporterBuilder Register<TDataImporter>(Func<IDataImporter> factory = null) where TDataImporter : IDataImporter;
    }
}
