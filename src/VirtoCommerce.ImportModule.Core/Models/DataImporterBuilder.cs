using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Security.Authorization;

namespace VirtoCommerce.ImportModule.Core.Models
{
    public sealed class DataImporterBuilder
    {
        private readonly Type _importerType;
        private readonly bool _clonesFactoryPrototype;

        /// <summary>
        /// The prototype holds the registration metadata; a non-null <paramref name="factory"/> creates it, and every importer
        /// is then a clone of it.
        /// </summary>
        public DataImporterBuilder(IServiceProvider serviceProvider, Type importerType, Func<IDataImporter> factory = null)
        {
            ServiceProvider = serviceProvider;
            _importerType = importerType;
            _clonesFactoryPrototype = factory != null;
            DataImporter = _clonesFactoryPrototype ? factory() : CreateImporter(serviceProvider, importerType);
        }

        public IDataImporter DataImporter { get; private set; }

        public IServiceProvider ServiceProvider { get; private set; }

        public DataImporterBuilder WithSettings(IEnumerable<SettingDescriptor> settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            DataImporter.AvailSettings = (DataImporter.AvailSettings ?? Array.Empty<SettingDescriptor>()).Concat(settings).ToArray();
            if (ServiceProvider != null)
            {
                var settingsRegistrar = ServiceProvider.GetRequiredService<ISettingsRegistrar>();
                settingsRegistrar.RegisterSettings(DataImporter.AvailSettings);
                settingsRegistrar.RegisterSettingsForType(DataImporter.AvailSettings, typeof(ImportProfile).Name);
            }
            return this;
        }

        public DataImporterBuilder WithAuthorizationReqirement(IAuthorizationRequirement authorizationReqirement)
        {
            DataImporter.AuthorizationRequirement = authorizationReqirement;

            return this;
        }
        public DataImporterBuilder WithAuthorizationPermission(string permission)
        {
            var permissionAuthorizationRequirement = new PermissionAuthorizationRequirement(permission);

            return WithAuthorizationReqirement(permissionAuthorizationRequirement);
        }

        public IDataImporter Build()
        {
            return DataImporter.Clone() as IDataImporter;
        }

        /// <summary>
        /// Builds the importer from <paramref name="serviceProvider"/>, which is expected to belong to the scope of the caller,
        /// so that the readers and writers the importer resolves are released with that scope.
        /// A builder created with a factory returns a clone of the factory's prototype instead.
        /// </summary>
        public IDataImporter Build(IServiceProvider serviceProvider)
        {
            if (_clonesFactoryPrototype)
            {
                return Build();
            }

            var importer = CreateImporter(serviceProvider, _importerType);
            importer.AuthorizationRequirement = DataImporter.AuthorizationRequirement;
            importer.AvailSettings = DataImporter.AvailSettings;

            return importer;
        }

        private static IDataImporter CreateImporter(IServiceProvider serviceProvider, Type importerType)
        {
            return serviceProvider is null
                ? Activator.CreateInstance(importerType) as IDataImporter
                : (IDataImporter)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, importerType);
        }
    }
}
