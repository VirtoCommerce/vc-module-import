using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IImportRunHistoryCrudService : ICrudService<ImportRunHistory>
    {
        public Task<ImportRunHistory> GetLastRun(string userId, string profileId);
    }
}
