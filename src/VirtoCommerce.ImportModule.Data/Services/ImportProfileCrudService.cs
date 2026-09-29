using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.ImportModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.GenericCrud;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public class ImportProfileCrudService : CrudService<ImportProfile, ImportProfileEntity,
         GenericChangedEntryEvent<ImportProfile>, GenericChangedEntryEvent<ImportProfile>>, IImportProfileCrudService
    {
        private readonly ISettingsManager _settingsManager;
        private readonly IDataImporterRegistrar _importersRegistry;

        public ImportProfileCrudService(
            IDataImporterRegistrar importersRegistry,
            Func<IImportRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IEventPublisher eventPublisher,
            ISettingsManager settingsManager)
            : base(repositoryFactory, platformMemoryCache, eventPublisher)
        {
            _settingsManager = settingsManager;
            _importersRegistry = importersRegistry;
        }

        protected override async Task<IList<ImportProfileEntity>> LoadEntities(IRepository repository, IList<string> ids, string responseGroup)
        {
            var result = await ((IImportRepository)repository).GetImportProfileByIds(ids.ToArray(), responseGroup);
            return result?.ToList();
        }

        protected override Task AfterDeleteAsync(IList<ImportProfile> models, IList<GenericChangedEntry<ImportProfile>> changedEntries)
        {
            return _settingsManager.DeepRemoveSettingsAsync(models);
        }

        protected override ImportProfile ProcessModel(string responseGroup, ImportProfileEntity entity, ImportProfile model)
        {
            _settingsManager.DeepLoadSettingsAsync(model).GetAwaiter().GetResult();

            var importer = _importersRegistry.AllRegisteredImporters.FirstOrDefault(x => x.TypeName == model.DataImporterType);
            if (importer != null && importer.AvailSettings != null && importer.AvailSettings.Any())
            {
                // Importer settings, plus the pipeline's own cursor settings, which no importer declares
                var names = importer.AvailSettings.Concat(ImportCursorSettings.AllSettings).Select(x => x.Name).ToHashSet();
                model.Settings = model.Settings.Where(x => names.Contains(x.Name)).ToList();
            }

            return model;
        }

        protected override async Task AfterSaveChangesAsync(IList<ImportProfile> models, IList<GenericChangedEntry<ImportProfile>> changedEntries)
        {
            await _settingsManager.DeepSaveSettingsAsync(models);
        }
    }
}
