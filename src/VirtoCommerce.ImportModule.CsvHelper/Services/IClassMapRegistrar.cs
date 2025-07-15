using System;

namespace VirtoCommerce.ImportModule.CsvHelper.Services
{
    public interface IClassMapRegistrar<TClassMap, TClass> where TClassMap : ClassMapExtended<TClass>
    {
        ClassMapBuilder<TClassMap, TClass> Register(Func<TClassMap> factory = null);
    }
}
