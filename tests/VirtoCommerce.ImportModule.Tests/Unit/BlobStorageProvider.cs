using System;
using System.IO;
using System.Threading.Tasks;
using VirtoCommerce.AssetsModule.Core.Assets;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class BlobStorageProvider : IBlobStorageProvider
    {
        public Stream OpenRead(string blobUrl)
        {
            return File.Open(blobUrl, FileMode.Open, FileAccess.Read);
        }

        public void Copy(string srcUrl, string destUrl)
        {
            throw new NotImplementedException();
        }

        public Task CopyAsync(string srcUrl, string destUrl)
        {
            throw new NotImplementedException();
        }

        public Task CreateFolderAsync(BlobFolder folder)
        {
            throw new NotImplementedException();
        }

        public Task<BlobInfo> GetBlobInfoAsync(string blobUrl)
        {
            throw new NotImplementedException();
        }

        public void Move(string srcUrl, string destUrl)
        {
            throw new NotImplementedException();
        }

        public Task MoveAsyncPublic(string srcUrl, string destUrl)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> OpenReadAsync(string blobUrl)
        {
            throw new NotImplementedException();
        }

        public Stream OpenWrite(string blobUrl)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> OpenWriteAsync(string blobUrl)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(string[] urls)
        {
            throw new NotImplementedException();
        }

        public Task<BlobEntrySearchResult> SearchAsync(string folderUrl, string keyword)
        {
            throw new NotImplementedException();
        }
    }
}
