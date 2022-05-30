using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;

namespace VirtoCommerce.ImportModule.Data.Importers
{
    public class TestDataWriter : IImportDataWriter
    {
        public Task WriteAsync(object[] items, ImportContext context)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
