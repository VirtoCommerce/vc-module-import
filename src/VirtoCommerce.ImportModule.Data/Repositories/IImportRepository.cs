using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ImportModule.Data.Repositories
{
    public interface IImportRepository : IRepository
    {
        IQueryable<ImportProfileEntity> ImportProfiles { get; }
        IQueryable<ImportRunHistoryEntity> ImportRunHistorys { get; }

        Task<ImportProfileEntity[]> GetImportProfileByIds(string[] ids, string responseGroup = null);
        Task<ImportRunHistoryEntity[]> GetImportRunHistoryByIds(string[] ids, string responseGroup = null);
    }
}
