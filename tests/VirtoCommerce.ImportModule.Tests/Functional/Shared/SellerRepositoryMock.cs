using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.MarketplaceVendorModule.Data.Infrastructure;
using VirtoCommerce.MarketplaceVendorModule.Data.Infrastructure.DataEntities;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;

namespace VirtoCommerce.MarketplaceVendorModule.Tests.Functional
{
    internal class SellerRepositoryMock : ISellerRepository
    {
        public IQueryable<SellerProductEntity> SellerProducts => throw new NotImplementedException();
        public static List<SellerProductEntity> SellerProductsEntity = new();
        public IQueryable<OfferEntity> Offers => throw new NotImplementedException();

        public IQueryable<ProductPublicationRequestEntity> PublicationRequests => throw new NotImplementedException();
        public static List<ProductPublicationRequestEntity> ProductsPublicationRequest = new();

        public IQueryable<ImportProfileEntity> ImportProfiles => throw new NotImplementedException();

        public IQueryable<ImportRunHistoryEntity> ImportRunHistorys => throw new NotImplementedException();

        public IUnitOfWork UnitOfWork => new Mock<IUnitOfWork>().Object;

        public void Add<T>(T item) where T : class
        {
            if (item.GetType() == typeof(SellerProductEntity))
            {
                var sellerProductEntity = item as SellerProductEntity;
                sellerProductEntity.Id = nameof(SellerProduct) + sellerProductEntity.OuterId;
                SellerProductsEntity.Add(sellerProductEntity);
            }
            else if (item.GetType() == typeof(ProductPublicationRequestEntity))
            {
                var productPublicationRequestEntity = item as ProductPublicationRequestEntity;
                productPublicationRequestEntity.Id = nameof(ProductPublicationRequest) + productPublicationRequestEntity.SellerProductId;
                ProductsPublicationRequest.Add(productPublicationRequestEntity);
            }
        }

        public void Attach<T>(T item) where T : class
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public Task<ImportProfileEntity[]> GetImportProfileByIds(string[] ids, string responseGroup = null)
        {
            throw new NotImplementedException();
        }

        public Task<ImportRunHistoryEntity[]> GetImportRunHistoryByIds(string[] ids, string responseGroup = null)
        {
            throw new NotImplementedException();
        }

        public Task<OfferEntity[]> GetOffersByIds(string[] ids, string responseGroup = null)
        {
            throw new NotImplementedException();
        }

        public Task<ProductPublicationRequestEntity[]> GetPublicationRequestsByIds(string[] ids, string responseGroup = null)
        {
            var result = Array.Empty<ProductPublicationRequestEntity>();
            if (!ids.IsNullOrEmpty())
            {
                result = ProductsPublicationRequest.Where(x => ids.Contains(x.Id)).ToArray();
            }
            return Task.FromResult(result);
        }

        public Task<SellerProductEntity[]> GetSellerProductsByIds(string[] ids, string responseGroup = null)
        {
            var result = Array.Empty<SellerProductEntity>();
            if (!ids.IsNullOrEmpty())
            {
                result = SellerProductsEntity.Where(x => ids.Contains(x.Id)).ToArray();
            }
            return Task.FromResult(result);
        }

        public void Remove<T>(T item) where T : class
        {
            throw new NotImplementedException();
        }

        public void Update<T>(T item) where T : class
        {
            throw new NotImplementedException();
        }
    }
}
