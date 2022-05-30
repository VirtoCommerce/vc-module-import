using System;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IImportDataReader : IDisposable
    {
        Task<int> GetTotalCountAsync(ImportContext context);
        Task<object[]> ReadNextPageAsync(ImportContext context);
        bool HasMoreResults { get; }
    }
}
