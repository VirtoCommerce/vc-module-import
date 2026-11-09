using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class DataImporterFactoryTests
    {
        [Fact]
        public void Disposing_The_Callers_Scope_Releases_The_Writer_The_Importer_Resolved()
        {
            using var root = CreateRootProvider(services =>
            {
                services.AddTransient<ScopeTestImporter>();
                services.AddTransient<ScopeTestWriter>();
            });
            root.GetRequiredService<IDataImporterRegistrar>().Register<ScopeTestImporter>();

            ScopeTestWriter writer;
            using (var scope = root.CreateScope())
            {
                var importer = scope.ServiceProvider.GetRequiredService<IDataImporterFactory>().Create(nameof(ScopeTestImporter));
                writer = (ScopeTestWriter)importer.OpenWriter(context: null);
            }

            // The test never disposes the writer itself: the container that resolved it is the only one that can,
            // so a writer still undisposed after the scope ends is one the root keeps until the process stops
            Assert.True(writer.IsDisposed);
        }

        [Fact]
        public void Create_Calls_A_Legacy_Factory_Once_And_Returns_Clones()
        {
            using var root = CreateRootProvider();
            var factoryCalls = 0;
#pragma warning disable CS0618 // The obsolete overload is the subject of this test
            root.GetRequiredService<IDataImporterRegistrar>().Register<LegacyTestImporter>(() =>
            {
                factoryCalls++;
                return new LegacyTestImporter { Marker = "factory" };
            });
#pragma warning restore CS0618

            using var scope = root.CreateScope();
            var importerFactory = scope.ServiceProvider.GetRequiredService<IDataImporterFactory>();
            var first = (LegacyTestImporter)importerFactory.Create(nameof(LegacyTestImporter));
            var second = (LegacyTestImporter)importerFactory.Create(nameof(LegacyTestImporter));

            // The marker exists only on the factory's instance, so an importer built from the scope instead of cloned lacks it
            Assert.Equal(1, factoryCalls);
            Assert.Equal("factory", first.Marker);
            Assert.Equal("factory", second.Marker);
            Assert.NotSame(first, second);
        }

        [Fact]
        public void Legacy_Register_With_A_Null_Factory_Builds_The_Importer_From_The_Callers_Scope()
        {
            using var root = CreateRootProvider();
#pragma warning disable CS0618 // The obsolete overload is the subject of this test
            root.GetRequiredService<IDataImporterRegistrar>().Register<NullFactoryTestImporter>(null);
#pragma warning restore CS0618

            using var scope = root.CreateScope();
            var importer = (NullFactoryTestImporter)scope.ServiceProvider.GetRequiredService<IDataImporterFactory>().Create(nameof(NullFactoryTestImporter));

            Assert.Same(scope.ServiceProvider, importer.ServiceProvider);
        }

        [Fact]
        public void Create_Finds_An_Importer_By_Class_Name_When_Its_TypeName_Differs()
        {
            using var root = CreateRootProvider();
            root.GetRequiredService<IDataImporterRegistrar>().Register<MismatchedTypeNameTestImporter>();

            using var scope = root.CreateScope();
            var importer = (MismatchedTypeNameTestImporter)scope.ServiceProvider.GetRequiredService<IDataImporterFactory>().Create(nameof(MismatchedTypeNameTestImporter));

            // A builder lookup that misses falls back to a clone of the root-built prototype, which has the right type
            // but the root provider
            Assert.Same(scope.ServiceProvider, importer.ServiceProvider);
        }

        private static ServiceProvider CreateRootProvider(Action<IServiceCollection> addServices = null)
        {
            var services = new ServiceCollection();
            addServices?.Invoke(services);
            services.AddSingleton<DataImporterRegistrar>();
            services.AddTransient<IDataImporterFactory, DataImporterFactory>();
            services.AddSingleton<IDataImporterRegistrar>(provider => provider.GetService<DataImporterRegistrar>());

            return services.BuildServiceProvider(validateScopes: true);
        }

        private abstract class TestImporterBase : IDataImporter
        {
            protected TestImporterBase(IServiceProvider serviceProvider = null)
            {
                ServiceProvider = serviceProvider;
            }

            public IServiceProvider ServiceProvider { get; }

            public virtual string TypeName => GetType().Name;
            public Dictionary<string, string> Metadata { get; } = new();
            public SettingDescriptor[] AvailSettings { get; set; }
            public IAuthorizationRequirement AuthorizationRequirement { get; set; }

            public IImportDataReader OpenReader(ImportContext context) => throw new NotSupportedException();
            public IImportDataWriter OpenWriter(ImportContext context) => throw new NotSupportedException();
            public Task<ValidationResult> ValidateAsync(ImportContext context) => Task.FromResult(new ValidationResult());
            public object Clone() => MemberwiseClone();
        }

        private sealed class LegacyTestImporter : TestImporterBase
        {
            public string Marker { get; set; }
        }

        private sealed class NullFactoryTestImporter : TestImporterBase
        {
            public NullFactoryTestImporter(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }

        private sealed class MismatchedTypeNameTestImporter : TestImporterBase
        {
            public MismatchedTypeNameTestImporter(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public override string TypeName => "SomeOtherName";
        }

        private sealed class ScopeTestImporter : IDataImporter
        {
            private readonly IServiceProvider _serviceProvider;

            public ScopeTestImporter(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public string TypeName => GetType().Name;
            public Dictionary<string, string> Metadata { get; } = new();
            public SettingDescriptor[] AvailSettings { get; set; }
            public IAuthorizationRequirement AuthorizationRequirement { get; set; }

            public IImportDataReader OpenReader(ImportContext context) => throw new NotSupportedException();
            public IImportDataWriter OpenWriter(ImportContext context) => _serviceProvider.GetRequiredService<ScopeTestWriter>();
            public Task<ValidationResult> ValidateAsync(ImportContext context) => Task.FromResult(new ValidationResult());
            public object Clone() => MemberwiseClone();
        }

        private sealed class ScopeTestWriter : IImportDataWriter
        {
            public bool IsDisposed { get; private set; }

            public Task WriteAsync(object[] items, ImportContext context) => Task.CompletedTask;

            public void Dispose()
            {
                IsDisposed = true;
            }
        }
    }
}
