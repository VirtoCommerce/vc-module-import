using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public class DataImporterRegistrar : IDataImporterRegistrar
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, DataImporterBuilder> _builders = new();
        private readonly object _lock = new();

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

        public DataImporterBuilder Register<TDataImporter>() where TDataImporter : IDataImporter
        {
            return RegisterBuilder<TDataImporter>(null);
        }

        [Obsolete("Use Register<TDataImporter>(). For custom construction register TDataImporter in the service collection with a factory (services.AddTransient<TDataImporter>(sp => ...)) and call Register<TDataImporter>().")]
        public DataImporterBuilder Register<TDataImporter>(Func<IDataImporter> factory) where TDataImporter : IDataImporter
        {
            return RegisterBuilder<TDataImporter>(factory);
        }

        internal DataImporterBuilder FindBuilder(Type type)
        {
            lock (_lock)
            {
                return _builders.GetValueOrDefault(type);
            }
        }

        private DataImporterBuilder RegisterBuilder<TDataImporter>(Func<IDataImporter> factory) where TDataImporter : IDataImporter
        {
            var typeInfo = AbstractTypeFactory<IDataImporter>.RegisterType<TDataImporter>();
            var builder = new DataImporterBuilder(_serviceProvider, typeof(TDataImporter), factory);
            lock (_lock)
            {
                _builders[typeof(TDataImporter)] = builder;
            }
            typeInfo.WithFactory(() => builder.Build());

            return builder;
        }
    }
}
