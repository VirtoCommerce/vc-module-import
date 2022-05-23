using System;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Data.Models;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public interface IImportDataReader : IDisposable
    {
        Task<int> GetTotalCountAsync(ImportContext context);
        Task<object[]> ReadNextPageAsync(ImportContext context);
        bool HasMoreResults { get; }
    }
}
