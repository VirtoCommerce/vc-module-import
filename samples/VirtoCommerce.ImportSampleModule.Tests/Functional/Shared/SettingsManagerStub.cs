using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportSampleModule.Tests.Functional.Shared
{
    public class SettingsManagerStub : ISettingsManager
    {
        public IEnumerable<SettingDescriptor> AllRegisteredSettings => throw new NotImplementedException();

        public Task<ObjectSettingEntry> GetObjectSettingAsync(string name, string objectType = null, string objectId = null)
        {
            return Task.FromResult(new ObjectSettingEntry(ModuleConstants.Settings.General.MaxErrorsCountThreshold));
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
