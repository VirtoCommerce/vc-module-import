using System;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IImportDataWriter : IDisposable
    {
        Task WriteAsync(object[] items, ImportContext context);

        /// <summary>
        /// Ensures all previously-issued writes are durable in the target store.
        /// Default: no-op — synchronous writers that commit inside WriteAsync need nothing.
        /// Concurrent / batched writers MUST override to await all in-flight work.
        /// Pipeline calls FlushAsync immediately before each cursor checkpoint and once in the finally block.
        /// </summary>
        Task FlushAsync(ImportContext context) => Task.CompletedTask;
    }
}
