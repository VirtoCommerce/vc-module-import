using System;
using VirtoCommerce.AssetsModule.Core.Assets;

namespace VirtoCommerce.MarketplaceVendorModule.Tests.Functional
{
    public class BlobUrlResolverStub : IBlobUrlResolver
    {
        public string GetAbsoluteUrl(string blobKey)
        {
            throw new NotImplementedException();
        }
    }
}
