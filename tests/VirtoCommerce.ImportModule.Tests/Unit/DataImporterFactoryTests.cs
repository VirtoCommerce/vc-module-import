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
            var services = new ServiceCollection();
            services.AddTransient<ScopeTestImporter>();
            services.AddTransient<ScopeTestWriter>();
            services.AddSingleton<DataImporterRegistrar>();
            services.AddSingleton<IDataImporterFactory>(provider => provider.GetService<DataImporterRegistrar>());
            services.AddSingleton<IDataImporterRegistrar>(provider => provider.GetService<DataImporterRegistrar>());
            using var root = services.BuildServiceProvider(validateScopes: true);
            root.GetRequiredService<IDataImporterRegistrar>().Register<ScopeTestImporter>(() => root.GetRequiredService<ScopeTestImporter>());

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
