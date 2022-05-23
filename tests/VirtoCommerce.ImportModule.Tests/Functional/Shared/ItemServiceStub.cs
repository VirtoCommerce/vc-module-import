using System;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Services;
using System.Collections.Generic;

namespace VirtoCommerce.MarketplaceVendorModule.Tests.Functional
{
    public class ItemServiceStub : IItemService
    {
        public static List<CatalogProduct> CatalogProducts = new();
        public Task DeleteAsync(string[] itemIds)
        {
            throw new NotImplementedException();
        }

        public Task<CatalogProduct> GetByIdAsync(string itemId, string responseGroup, string catalogId = null)
        {
            throw new NotImplementedException();
        }

        public Task<CatalogProduct[]> GetByIdsAsync(string[] itemIds, string respGroup, string catalogId = null)
        {
            return Task.FromResult(CatalogProducts.ToArray());
        }

        public Task SaveChangesAsync(CatalogProduct[] items)
        {
            foreach (var item in items)
            {
                if(item.Id == null)
                    item.Id = Guid.NewGuid().ToString();
                item.Category = new Category { OuterId = "TestCategoryOuterId" };
                CatalogProducts.Add(item);
            }
            return Task.CompletedTask;
        }
    }
}
