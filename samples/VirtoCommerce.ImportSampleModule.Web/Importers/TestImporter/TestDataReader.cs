using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class TestDataReader : IImportDataReader
    {
        private readonly object _totalCount;
        private readonly object _isErrors;
        private int _processedCount = 0;

        public bool HasMoreResults { get; private set; } = true;

        public TestDataReader(ImportContext context)
        {
            _totalCount = context.ImportProfile.Settings.Where(x => x.Name == TestSettings.TotalCount.Name).FirstOrDefault()?.Value ?? TestSettings.TotalCount.DefaultValue;
            _isErrors = context.ImportProfile.Settings.Where(x => x.Name == TestSettings.IsErrors.Name).FirstOrDefault()?.Value ?? false;
        }

        public Task<int> GetTotalCountAsync(ImportContext context)
        {
            return Task.FromResult(Convert.ToInt32(_totalCount));
        }

        public async Task<object[]> ReadNextPageAsync(ImportContext context)
        {
            var results = new List<object>();
            await Task.Delay(100);
            results.Add(_processedCount++);

            if (Convert.ToBoolean(_isErrors) && _processedCount % 100 == 0)
            {
                var errorInfo = new ErrorInfo
                {
                    ErrorMessage = $"Error on line {_processedCount}",
                    ErrorCode = "ImportError"
                };
                context.ErrorCallback(errorInfo);
            }

            if (_processedCount == Convert.ToInt32(_totalCount))
                HasMoreResults = false;
            return results.ToArray();
        }
        public void Dispose()
        {
        }
    }
}
