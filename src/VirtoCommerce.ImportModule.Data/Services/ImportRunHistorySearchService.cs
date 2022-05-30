using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.ImportModule.Core.Domains;
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
    public class ImportRunHistorySearchService : SearchService<SearchImportRunHistoryCriteria, SearchImportRunHistoryResult, ImportRunHistory, ImportRunHistoryEntity>, IImportRunHistorySearchService
    {
        public ImportRunHistorySearchService(
            Func<IImportRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            ICrudService<ImportRunHistory> crudService)
            : base(repositoryFactory, platformMemoryCache, crudService)
        {
        }
        protected override IQueryable<ImportRunHistoryEntity> BuildQuery(IRepository repository, SearchImportRunHistoryCriteria criteria)
        {
            var query = ((IImportRepository)repository).ImportRunHistorys;

            if (!string.IsNullOrEmpty(criteria.SellerId))
            {
                query = query.Where(x => x.SellerId == criteria.SellerId);
            }

            if (!string.IsNullOrEmpty(criteria.ProfileId))
            {
                query = query.Where(x => x.ProfileId == criteria.ProfileId);
            }

            if (!string.IsNullOrEmpty(criteria.JobId))
            {
                query = query.Where(x => x.JobId == criteria.JobId);
            }

            return query;
        }

        protected override IList<SortInfo> BuildSortExpression(SearchImportRunHistoryCriteria criteria)
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
