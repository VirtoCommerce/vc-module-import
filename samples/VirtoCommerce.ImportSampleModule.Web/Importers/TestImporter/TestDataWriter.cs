using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
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
