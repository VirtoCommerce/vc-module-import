using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.MarketplaceVendorModule.Data.Infrastructure;
using VirtoCommerce.MarketplaceVendorModule.Data.Infrastructure.DataEntities;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Data.GenericCrud;

namespace VirtoCommerce.ImportModule.Data.Queries.SearchImportProfilesHistory
{
    public class ImportRunHistorySearchService : SearchService<SearchImportProfilesHistoryQuery, SearchImportProfilesHistoryResult, ImportRunHistory, ImportRunHistoryEntity>, IImportRunHistorySearchService
    {
        public ImportRunHistorySearchService(
            Func<ISellerRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            ICrudService<ImportRunHistory> crudService)
            : base(repositoryFactory, platformMemoryCache, crudService)
        {
        }
        protected override IQueryable<ImportRunHistoryEntity> BuildQuery(IRepository repository, SearchImportProfilesHistoryQuery criteria)
        {
            var query = ((ISellerRepository)repository).ImportRunHistorys;

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

        protected override IList<SortInfo> BuildSortExpression(SearchImportProfilesHistoryQuery criteria)
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
