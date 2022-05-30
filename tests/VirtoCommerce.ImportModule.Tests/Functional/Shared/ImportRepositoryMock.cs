using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.ImportModule.Data.Infrastructure.DataEntities;
using VirtoCommerce.ImportModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Domain;

namespace VirtoCommerce.ImportModule.Tests.Functional
{
    internal class ImportRepositoryMock : IImportRepository
    {
        public IQueryable<ImportProfileEntity> ImportProfiles => throw new NotImplementedException();

        public IQueryable<ImportRunHistoryEntity> ImportRunHistorys => throw new NotImplementedException();

        public IUnitOfWork UnitOfWork => new Mock<IUnitOfWork>().Object;

        public void Add<T>(T item) where T : class
        {
            throw new NotImplementedException();
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
