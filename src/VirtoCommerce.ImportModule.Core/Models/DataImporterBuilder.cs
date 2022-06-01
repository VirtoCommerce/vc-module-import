using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Core.Models
{
    public sealed class DataImporterBuilder
    {
        public DataImporterBuilder(IServiceProvider serviceProvider, Type importerType, Func<IDataImporter> factory = null)
        {
            ServiceProvider = serviceProvider;
            DataImporter = factory != null ? factory() : Activator.CreateInstance(importerType) as IDataImporter;
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

        public IDataImporter Build()
        {
            return DataImporter.Clone() as IDataImporter;
        }
    }
}
