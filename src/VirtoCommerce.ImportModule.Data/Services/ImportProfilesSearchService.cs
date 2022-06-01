using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Models.Search;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Infrastructure.DataEntities;
using VirtoCommerce.ImportModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Data.GenericCrud;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public class ImportProfilesSearchService : SearchService<SearchImportProfilesCriteria, SearchImportProfilesResult, ImportProfile, ImportProfileEntity>, IImportProfilesSearchService
    {
        public ImportProfilesSearchService(
            Func<IImportRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            ICrudService<ImportProfile> crudService)
            : base(repositoryFactory, platformMemoryCache, crudService)
        {
        }
        protected override IQueryable<ImportProfileEntity> BuildQuery(IRepository repository, SearchImportProfilesCriteria criteria)
        {
            var query = ((IImportRepository)repository).ImportProfiles;

            if (!string.IsNullOrEmpty(criteria.SellerId))
            {
                query = query.Where(x => x.SellerId == criteria.SellerId);
            }

            if (!string.IsNullOrEmpty(criteria.Name))
            {
                query = query.Where(x => x.Name == criteria.Name);
            }

            return query;
        }

        protected override IList<SortInfo> BuildSortExpression(SearchImportProfilesCriteria criteria)
        {
            var sortInfos = criteria.SortInfos;
            if (sortInfos.IsNullOrEmpty())
            {
                sortInfos = new[]
                {
                    new SortInfo
                    {
                        SortColumn = ReflectionUtility.GetPropertyName<ImportProfileEntity>(x => x.CreatedDate),
                        SortDirection = SortDirection.Descending
                    }
                };
            }

            return sortInfos;
        }
    }
}
