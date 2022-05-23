using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public class DataImporterRegistrar : IDataImporterRegistrar, IDataImporterFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public IEnumerable<IDataImporter> AllRegisteredImporters
        {
            get
            {
                return AbstractTypeFactory<IDataImporter>.AllTypeInfos.Select(x => AbstractTypeFactory<IDataImporter>.TryCreateInstance(x.TypeName));
            }
        }

        public DataImporterRegistrar(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public DataImporterBuilder Register<TDataImporter>(Func<IDataImporter> factory = null) where TDataImporter : IDataImporter
        {
            var typeInfo = AbstractTypeFactory<IDataImporter>.RegisterType<TDataImporter>();
            var builder = new DataImporterBuilder(_serviceProvider, typeof(TDataImporter), factory);
            typeInfo.WithFactory(() => builder.Build());
            return builder;
        }

        public IDataImporter Create(string typeName)
        {
            return AbstractTypeFactory<IDataImporter>.TryCreateInstance(typeName);
        }
    }
}
