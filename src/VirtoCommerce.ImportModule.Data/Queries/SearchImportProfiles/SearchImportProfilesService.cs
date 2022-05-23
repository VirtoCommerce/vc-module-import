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

namespace VirtoCommerce.ImportModule.Data.Queries.SearchImportProfiles
{
    public class SearchImportProfilesService : SearchService<SearchImportProfilesQuery, SearchImportProfilesResult, ImportProfile, ImportProfileEntity>, ISearchImportProfilesService
    {
        public SearchImportProfilesService(
            Func<ISellerRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            ICrudService<ImportProfile> crudService)
            : base(repositoryFactory, platformMemoryCache, crudService)
        {
        }
        protected override IQueryable<ImportProfileEntity> BuildQuery(IRepository repository, SearchImportProfilesQuery criteria)
        {
            var query = ((ISellerRepository)repository).ImportProfiles;            

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

        protected override IList<SortInfo> BuildSortExpression(SearchImportProfilesQuery criteria)
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
