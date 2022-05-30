using System;
using VirtoCommerce.AssetsModule.Core.Assets;

namespace VirtoCommerce.ImportModule.Tests.Functional
{
    public class BlobUrlResolverStub : IBlobUrlResolver
    {
        public string GetAbsoluteUrl(string blobKey)
        {
            throw new NotImplementedException();
        }
    }
}
