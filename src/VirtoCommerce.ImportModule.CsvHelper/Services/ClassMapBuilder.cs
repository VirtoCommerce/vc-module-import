using System;
using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.CsvHelper.Services
{
    public class ClassMapBuilder<TClassMap, TClass> where TClassMap : ClassMapExtended<TClass>
    {
        public ClassMapBuilder() { }
        public ClassMapBuilder(IServiceProvider serviceProvider, Type classMapType, Func<TClassMap> factory = null)
        {
            ServiceProvider = serviceProvider;
            ClassMap = factory != null ? factory() : Activator.CreateInstance(classMapType) as TClassMap;
        }

        public TClassMap ClassMap { get; private set; }

        public IServiceProvider ServiceProvider { get; private set; }

        public virtual ClassMapBuilder<TClassMap, TClass> WithSettings(ICollection<ObjectSettingEntry> settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            ClassMapExtended<TClass>.Settings = settings;
            return this;
        }

        public TClassMap Build()
        {
            return ClassMap;
        }
    }
}
