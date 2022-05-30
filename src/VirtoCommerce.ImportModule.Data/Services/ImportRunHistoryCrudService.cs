using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.ImportModule.Core.Domains;
using VirtoCommerce.ImportModule.Data.Infrastructure.DataEntities;
using VirtoCommerce.ImportModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public class ImportRunHistoryCrudService : AggregateRootCrudServiceBase<ImportRunHistory, ImportRunHistoryEntity>
    {
        public ImportRunHistoryCrudService(
            IMediator mediator,
            Func<IImportRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IEventPublisher eventPublisher
            )
            : base(mediator, repositoryFactory, platformMemoryCache, eventPublisher)
        {
        }

        protected override async Task<IEnumerable<ImportRunHistoryEntity>> LoadEntities(IRepository repository, IEnumerable<string> ids, string responseGroup)
        {
            return await ((IImportRepository)repository).GetImportRunHistoryByIds(ids.ToArray(), responseGroup);
        }

    }
}
