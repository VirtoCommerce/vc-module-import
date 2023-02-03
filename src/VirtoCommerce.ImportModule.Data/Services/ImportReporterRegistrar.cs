using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public class ImportReporterRegistrar : IImportReporterRegistrar, IImportReporterFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ImportReporterRegistrar(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IEnumerable<IImportReporter> AllRegisteredReporters
        {
            get
            {
                return AbstractTypeFactory<IImportReporter>.AllTypeInfos.Select(x => AbstractTypeFactory<IImportReporter>.TryCreateInstance(x.TypeName));
            }
        }

        public ImportReporterBuilder Register<TImportReporter>(Func<IImportReporter> factory = null) where TImportReporter : IImportReporter
        {
            var typeInfo = AbstractTypeFactory<IImportReporter>.RegisterType<TImportReporter>();
            var builder = new ImportReporterBuilder(_serviceProvider, typeof(TImportReporter), factory);
            typeInfo.WithFactory(() => builder.Build());
            return builder;
        }

        public IImportReporter Create(string typeName)
        {
            return AbstractTypeFactory<IImportReporter>.TryCreateInstance(typeName);
        }
    }
}
