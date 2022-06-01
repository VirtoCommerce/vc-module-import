using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Data.Infrastructure.DataEntities;
using VirtoCommerce.ImportModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Data.GenericCrud;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public class ImportRunHistoryCrudService : CrudService<ImportRunHistory, ImportRunHistoryEntity,
         GenericChangedEntryEvent<ImportRunHistory>, GenericChangedEntryEvent<ImportRunHistory>>
    {
        public ImportRunHistoryCrudService(
            Func<IImportRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IEventPublisher eventPublisher
            )
            : base(repositoryFactory, platformMemoryCache, eventPublisher)
        {
        }

        protected override async Task<IEnumerable<ImportRunHistoryEntity>> LoadEntities(IRepository repository, IEnumerable<string> ids, string responseGroup)
        {
            return await ((IImportRepository)repository).GetImportRunHistoryByIds(ids.ToArray(), responseGroup);
        }

    }
}
