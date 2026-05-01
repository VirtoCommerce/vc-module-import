using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportDataWriterFlushTests
    {
        private sealed class NoopWriter : IImportDataWriter
        {
            public Task WriteAsync(object[] items, ImportContext context) => Task.CompletedTask;
            public void Dispose() { }
        }

        [Fact]
        public async Task Default_FlushAsync_Is_Noop_For_Synchronous_Writers()
        {
            IImportDataWriter writer = new NoopWriter();
            var profile = new ImportProfile();

            var ex = await Record.ExceptionAsync(() => writer.FlushAsync(new ImportContext(profile)));

            Assert.Null(ex);
        }
    }
}
