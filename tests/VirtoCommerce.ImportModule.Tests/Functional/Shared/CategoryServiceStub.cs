using System;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Services;

namespace VirtoCommerce.MarketplaceVendorModule.Tests.Functional
{
    public class CategoryServiceStub : ICategoryService
    {
        public Task DeleteAsync(string[] categoryIds)
        {
            throw new NotImplementedException();
        }

        public Task<Category[]> GetByIdsAsync(string[] categoryIds, string responseGroup, string catalogId = null)
        {
            return Task.FromResult(new[] { new Category() });
        }

        public Task SaveChangesAsync(Category[] categories)
        {
            throw new NotImplementedException();
        }
    }
}
