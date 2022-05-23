using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.ImportModule.Data.Services;

namespace VirtoCommerce.ImportModule.Data.Queries.PreviewData
{
    public class PreviewDataQueryHandler : IQueryHandler<PreviewDataQuery, ImportDataPreview>
    {
        private readonly IDataImporterFactory _dataImporterFactory;

        public PreviewDataQueryHandler(IDataImporterFactory dataImporterFactory)
        {
            _dataImporterFactory = dataImporterFactory;
        }

        public async Task<ImportDataPreview> Handle(PreviewDataQuery request, CancellationToken cancellationToken)
        {
            var importer = _dataImporterFactory.Create(request.ImportProfile.DataImporterType);
            var context = new ImportContext(request.ImportProfile);

            using var reader = importer.OpenReader(context);

            var records = new List<object>();

            do
            {
                records.AddRange(await reader.ReadNextPageAsync(context));

            } while (reader.HasMoreResults && records.Count < request.ImportProfile.PreviewObjectCount);

            var result = new ImportDataPreview
            {
                TotalCount = await reader.GetTotalCountAsync(context),
                Records = records.Take(request.ImportProfile.PreviewObjectCount).ToArray(),
            };

            return result;
        }
    }
}
