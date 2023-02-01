using System;
using System.Collections.Generic;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Core.Models
{
    public sealed class ImportReporterBuilder
    {
        public ImportReporterBuilder(IServiceProvider serviceProvider, Type reporterType, Func<IImportReporter> factory = null)
        {
            ServiceProvider = serviceProvider;
            ImportReporter = factory != null ? factory() : Activator.CreateInstance(reporterType) as IImportReporter;
        }

        public IImportReporter ImportReporter { get; private set; }

        public IServiceProvider ServiceProvider { get; private set; }

        public ImportReporterBuilder WithSettings(IEnumerable<SettingDescriptor> settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            //ImportReporter.AvailSettings = (ImportReporter.AvailSettings ?? Array.Empty<SettingDescriptor>()).Concat(settings).ToArray();
            //if (ServiceProvider != null)
            //{
            //    var settingsRegistrar = ServiceProvider.GetRequiredService<ISettingsRegistrar>();
            //    settingsRegistrar.RegisterSettings(ImportReporter.AvailSettings);
            //    settingsRegistrar.RegisterSettingsForType(ImportReporter.AvailSettings, typeof(ImportProfile).Name);
            //}
            return this;
        }

        public IImportReporter Build()
        {
            return ImportReporter.Clone() as IImportReporter;
        }
    }
}
