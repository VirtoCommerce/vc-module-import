using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ImportModule.CsvHelper.Services
{
    public class ClassMapRegistrar<TClassMap, TClass> : IClassMapRegistrar<TClassMap, TClass>, IClassMapFactory<TClassMap> where TClassMap : ClassMapExtended<TClass>
    {
        private readonly IServiceProvider _serviceProvider;

        public ClassMapRegistrar() { }
        public ClassMapRegistrar(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public TClassMap Create(string typeName)
        {
            return AbstractTypeFactory<TClassMap>.TryCreateInstance(typeName);
        }

        public virtual ClassMapBuilder<TClassMap, TClass> Register(Func<TClassMap> factory = null)
        {
            var typeInfo = AbstractTypeFactory<TClassMap>.RegisterType<TClassMap>();
            var builder = new ClassMapBuilder<TClassMap, TClass>(_serviceProvider, typeof(TClassMap), factory);
            typeInfo.WithFactory(() => builder.Build());
            return builder;
        }
    }
}
