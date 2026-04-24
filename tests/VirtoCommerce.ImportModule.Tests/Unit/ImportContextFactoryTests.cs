using System.Threading;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportContextFactoryTests
    {
        public sealed class TestImportContext : ImportContext
        {
            public TestImportContext(ImportProfile profile) : base(profile) { }

            public string Tag { get; set; }
        }

        [Fact]
        public async Task ImportAsync_resolves_context_via_AbstractTypeFactory_and_calls_importer_with_derived_type()
        {
            // Register override before test run; clean up after to avoid polluting global factory state.
            AbstractTypeFactory<ImportContext>.OverrideType<ImportContext, TestImportContext>();
            try
            {
                ImportContext capturedContext = null;

                var importer = new Mock<IDataImporter>();
                importer.Setup(x => x.OpenReaderAsync(It.IsAny<ImportContext>()))
                    .Callback<ImportContext>(ctx => capturedContext = ctx)
                    .ReturnsAsync(new NoopReader());
                importer.Setup(x => x.OpenWriterAsync(It.IsAny<ImportContext>()))
                    .ReturnsAsync(new NoopWriter());
                importer.Setup(x => x.Clone()).Returns(() => importer.Object);

                var importerFactory = new Mock<IDataImporterFactory>();
                importerFactory.Setup(x => x.Create(It.IsAny<string>())).Returns(importer.Object);

                var estimator = new Mock<IImportRemainingEstimatorFactory>();
                estimator.Setup(x => x.Create(It.IsAny<string>())).Returns(Mock.Of<IImportRemainingEstimator>());
                var reporter = new Mock<IImportReporterFactory>();
                reporter.Setup(x => x.Create(It.IsAny<string>())).Returns(Mock.Of<IImportReporter>());

                var manager = TestHelperFactory.CreateManager(
                    factory: importerFactory.Object,
                    estimator: estimator.Object,
                    reporter: reporter.Object);

                var profile = new ImportProfile { DataImporterType = "noop" };

                await manager.ImportAsync(profile, _ => Task.CompletedTask, CancellationToken.None);

                Assert.NotNull(capturedContext);
                var derived = Assert.IsType<TestImportContext>(capturedContext);
                Assert.Same(profile, derived.ImportProfile);
            }
            finally
            {
                // Restore base registration so sibling tests see default behavior.
                AbstractTypeFactory<ImportContext>.OverrideType<TestImportContext, ImportContext>();
            }
        }

        private sealed class NoopReader : IImportDataReader
        {
            public bool HasMoreResults => false;
            public Task<int> GetTotalCountAsync(ImportContext context) => Task.FromResult(0);
            public Task<object[]> ReadNextPageAsync(ImportContext context) => Task.FromResult(System.Array.Empty<object>());
            public void Dispose() { }
        }

        private sealed class NoopWriter : IImportDataWriter
        {
            public Task WriteAsync(object[] items, ImportContext context) => Task.CompletedTask;
            public Task FlushAsync(ImportContext context) => Task.CompletedTask;
            public void Dispose() { }
        }
    }
}
