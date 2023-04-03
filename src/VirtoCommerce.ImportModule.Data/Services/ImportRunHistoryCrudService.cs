using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.ImportModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Data.GenericCrud;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public class ImportRunHistoryCrudService : CrudService<ImportRunHistory, ImportRunHistoryEntity,
         GenericChangedEntryEvent<ImportRunHistory>, GenericChangedEntryEvent<ImportRunHistory>>, IImportRunHistoryCrudService
    {
        private readonly Func<IImportRepository> _crudService;
        public ImportRunHistoryCrudService(
            Func<IImportRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IEventPublisher eventPublisher
            )
            : base(repositoryFactory, platformMemoryCache, eventPublisher)
        {
            _crudService = repositoryFactory;
        }

        public async Task<ImportRunHistory> GetLastRun(string userId, string profileId)
        {
            using (var rep = _crudService())
            {
                var result = await rep.ImportRunHistories.OrderByDescending(x => x.CreatedDate).Where(x => x.UserId == userId && x.ProfileId == profileId).FirstOrDefaultAsync();
                return result?.ToModel(new ImportRunHistory());
            }
        }

        protected override async Task<IEnumerable<ImportRunHistoryEntity>> LoadEntities(IRepository repository, IEnumerable<string> ids, string responseGroup)
        {
            return await ((IImportRepository)repository).GetImportRunHistoryByIds(ids.ToArray(), responseGroup);
        }

    }
}
