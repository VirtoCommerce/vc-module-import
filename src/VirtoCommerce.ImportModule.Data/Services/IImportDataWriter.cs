using System;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Data.Models;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public interface IImportDataWriter : IDisposable
    {
        Task WriteAsync(object[] items, ImportContext context);
    }
}
