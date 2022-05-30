using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.ImportModule.Core.Domains;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Infrastructure.DataEntities;
using VirtoCommerce.ImportModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public class ImportProfileCrudService : AggregateRootCrudServiceBase<ImportProfile, ImportProfileEntity>
    {
        private readonly ISettingsManager _settingsManager;
        private readonly IDataImporterRegistrar _importersRegistry;

        public ImportProfileCrudService(
            IMediator mediator,
            IDataImporterRegistrar importersRegistry,
            Func<IImportRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IEventPublisher eventPublisher,
            ISettingsManager settingsManager)
            : base(mediator, repositoryFactory, platformMemoryCache, eventPublisher)
        {
            _settingsManager = settingsManager;
            _importersRegistry = importersRegistry;
        }

        protected override async Task<IEnumerable<ImportProfileEntity>> LoadEntities(IRepository repository, IEnumerable<string> ids, string responseGroup)
        {
            return await ((IImportRepository)repository).GetImportProfileByIds(ids.ToArray(), responseGroup);
        }

        protected override Task AfterDeleteAsync(IEnumerable<ImportProfile> models, IEnumerable<GenericChangedEntry<ImportProfile>> changedEntries)
        {
            return _settingsManager.DeepRemoveSettingsAsync(models);
        }

        protected override ImportProfile ProcessModel(string responseGroup, ImportProfileEntity entity, ImportProfile model)
        {
            _settingsManager.DeepLoadSettingsAsync(model).GetAwaiter().GetResult();

            var importer = _importersRegistry.AllRegisteredImporters.FirstOrDefault(x => x.TypeName == model.DataImporterType);
            //filter only settings that defined for a importer
            model.Settings = model.Settings.Join(importer.AvailSettings, entry => entry.Name, setting => setting.Name, (entry, setting) => entry).ToList();

            return model;
        }

        protected override async Task AfterSaveChangesAsync(IEnumerable<ImportProfile> models, IEnumerable<GenericChangedEntry<ImportProfile>> changedEntries)
        {
            await DispathDomainEvents(models);
            await _settingsManager.DeepSaveSettingsAsync(models);
        }
    }
}
