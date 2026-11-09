using System;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ImportModule.Data.Services
{
    /// <summary>
    /// Creates importers from the provider of the scope this factory was resolved from.
    /// </summary>
    public class DataImporterFactory : IDataImporterFactory
    {
        private readonly DataImporterRegistrar _registrar;
        private readonly IServiceProvider _serviceProvider;

        public DataImporterFactory(DataImporterRegistrar registrar, IServiceProvider serviceProvider)
        {
            _registrar = registrar;
            _serviceProvider = serviceProvider;
        }

        public virtual IDataImporter Create(string typeName)
        {
            var typeInfo = AbstractTypeFactory<IDataImporter>.FindTypeInfoByName(typeName);
            var builder = typeInfo is null ? null : _registrar.FindBuilder(typeInfo.Type);

            // An unknown name throws OperationCanceledException here, which ImportController.CreateImporter maps to BadRequest;
            // a type overridden without a registrar builder is created by the type factory
            if (builder is null)
            {
                return AbstractTypeFactory<IDataImporter>.TryCreateInstance(typeName);
            }

            var importer = builder.Build(_serviceProvider);
            typeInfo.SetupAction?.Invoke(importer);

            return importer;
        }
    }
}
