using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportSampleModule.Web.Importers;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportSampleModule.Tests.Functional.Shared
{
    public class SettingsManagerStub : ISettingsManager
    {
        public IEnumerable<SettingDescriptor> AllRegisteredSettings => throw new NotImplementedException();

        public IEnumerable<ObjectSettingEntry> AllSettings => new List<ObjectSettingEntry>
        {
            new(ModuleConstants.Settings.General.MaxErrorsCountThreshold) { Value = 1 },
            new(ModuleConstants.Settings.General.DefaultImportReporter) { Value = nameof(TestDataReporter) },
            new(ModuleConstants.Settings.General.RemainingEstimator) { Value = nameof(TestRemainingEstimator) }
        };

        public Task<ObjectSettingEntry> GetObjectSettingAsync(string name, string objectType = null, string objectId = null)
        {
            var setting = AllSettings.FirstOrDefault(x => x.Name == name);
            return Task.FromResult(setting);
        }

        public Task<IEnumerable<ObjectSettingEntry>> GetObjectSettingsAsync(IEnumerable<string> names, string objectType = null, string objectId = null)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<SettingDescriptor> GetSettingsForType(string typeName)
        {
            throw new NotImplementedException();
        }

        public void RegisterSettings(IEnumerable<SettingDescriptor> settings, string moduleId = null)
        {
            throw new NotImplementedException();
        }

        public void RegisterSettingsForType(IEnumerable<SettingDescriptor> settings, string typeName)
        {
            throw new NotImplementedException();
        }

        public Task RemoveObjectSettingsAsync(IEnumerable<ObjectSettingEntry> objectSettings)
        {
            throw new NotImplementedException();
        }

        public Task SaveObjectSettingsAsync(IEnumerable<ObjectSettingEntry> objectSettings)
        {
            throw new NotImplementedException();
        }
    }
}
