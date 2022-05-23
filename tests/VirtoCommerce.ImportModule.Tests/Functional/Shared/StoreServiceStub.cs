using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Data.GenericCrud;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.MarketplaceVendorModule.Tests.Functional
{
    public class StoreServiceStub : ICrudService<Store>, IStoreService
    {
        public Task DeleteAsync(IEnumerable<string> ids, bool softDelete = false)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(string[] ids)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<Store>> GetAsync(List<string> ids, string responseGroup = null)
        {
            throw new NotImplementedException();
        }

        public Task<Store> GetByIdAsync(string id, string responseGroup = null)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Store>> GetByIdsAsync(IEnumerable<string> ids, string responseGroup = null)
        {
            throw new NotImplementedException();
        }

        public Task<Store[]> GetByIdsAsync(string[] ids, string responseGroup = null)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetUserAllowedStoreIdsAsync(ApplicationUser user)
        {
            throw new NotImplementedException();
        }

        public Task SaveChangesAsync(IEnumerable<Store> models)
        {
            throw new NotImplementedException();
        }

        public Task SaveChangesAsync(Store[] stores)
        {
            throw new NotImplementedException();
        }
    }
}
