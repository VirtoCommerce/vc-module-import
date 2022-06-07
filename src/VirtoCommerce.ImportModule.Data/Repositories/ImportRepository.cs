using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Data.Infrastructure;

namespace VirtoCommerce.ImportModule.Data.Repositories
{
    public class ImportRepository : DbContextRepositoryBase<ImportDbContext>, IImportRepository
    {
        public ImportRepository(ImportDbContext dbContext)
            : base(dbContext)
        {
        }

        public IQueryable<ImportProfileEntity> ImportProfiles => DbContext.Set<ImportProfileEntity>();
        public IQueryable<ImportRunHistoryEntity> ImportRunHistorys => DbContext.Set<ImportRunHistoryEntity>();

        public async Task<ImportProfileEntity[]> GetImportProfileByIds(string[] ids, string responseGroup = null)
        {
            var result = Array.Empty<ImportProfileEntity>();

            if (!ids.IsNullOrEmpty())
            {
                result = await ImportProfiles.Where(x => ids.Contains(x.Id)).ToArrayAsync();
            }
            return result;
        }

        public async Task<ImportRunHistoryEntity[]> GetImportRunHistoryByIds(string[] ids, string responseGroup = null)
        {
            var result = Array.Empty<ImportRunHistoryEntity>();

            if (!ids.IsNullOrEmpty())
            {
                result = await ImportRunHistorys.Where(x => ids.Contains(x.Id)).ToArrayAsync();
            }
            return result;
        }
    }
}
