using System;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IImportDataWriter : IDisposable
    {
        Task WriteAsync(object[] items, ImportContext context);
    }
}
