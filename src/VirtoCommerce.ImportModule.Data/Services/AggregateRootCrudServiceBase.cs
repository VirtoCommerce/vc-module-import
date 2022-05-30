using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Data.GenericCrud;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public abstract class AggregateRootCrudServiceBase<TModel, TEntity> : CrudService<TModel,
         TEntity,
         GenericChangedEntryEvent<TModel>,
         GenericChangedEntryEvent<TModel>>
        where TModel : AggregateRoot
        where TEntity : Entity, IDataEntity<TEntity, TModel>
    {
        private readonly IMediator _mediator;

        protected AggregateRootCrudServiceBase(
            IMediator mediator,
            Func<IImportRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IEventPublisher eventPublisher)
            : base(repositoryFactory, platformMemoryCache, eventPublisher)
        {
            _mediator = mediator;
        }

        protected override async Task AfterDeleteAsync(IEnumerable<TModel> models, IEnumerable<GenericChangedEntry<TModel>> changedEntries)
        {
            await base.AfterDeleteAsync(models, changedEntries);
            await DispathDomainEvents(models);
        }
        protected override async Task AfterSaveChangesAsync(IEnumerable<TModel> models, IEnumerable<GenericChangedEntry<TModel>> changedEntries)
        {
            await base.AfterSaveChangesAsync(models, changedEntries);
            await DispathDomainEvents(models);
        }

        protected virtual async Task DispathDomainEvents(IEnumerable<TModel> models)
        {
            // Dispatch Domain Events collection. 
            //TODO: generalize this code
            var domainEvents = models.SelectMany(x => x.DomainEvents).ToList();
            models.ToList().ForEach(entity => entity.ClearDomainEvents());
            if (!EventSuppressor.EventsSuppressed)
            {
                foreach (var domainEvent in domainEvents)
                {
                    await _mediator.Publish(domainEvent);
                }
            }
        }
    }
}
