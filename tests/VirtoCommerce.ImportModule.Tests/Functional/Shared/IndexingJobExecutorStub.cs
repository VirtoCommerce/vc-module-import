using System.Collections.Generic;
using VirtoCommerce.MarketplaceVendorModule.Data.BackgroundJobs;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.MarketplaceVendorModule.Tests.Functional
{
    public class IndexingJobExecutorStub : IIndexingJobExecutor
    {
        public void EnqueueIndexAndDeleteDocuments(IndexEntry[] indexEntries, string priority = "default", IList<IIndexDocumentBuilder> builders = null)
        {
        }
    }
}
