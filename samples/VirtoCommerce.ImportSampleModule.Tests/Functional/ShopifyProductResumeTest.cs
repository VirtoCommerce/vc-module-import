using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportSampleModule.Web.Importers;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.ImportSampleModule.Tests.Functional
{
    public class ShopifyProductResumeTest
    {
        [Fact]
        public async Task ShopifyProductReader_ResumesAfterCursorRestore()
        {
            // Arrange — phase 1: read first page (rows h1, h2)
            var filePath = TestHepler.GetFilePath("resume_shopify_products.csv");
            var profile = CreateImportProfile(filePath);

            var context1 = new ImportContext(profile);
            await using var stream1 = File.OpenRead(filePath);
            using var reader1 = new ShopifyProductDataReader(stream1, context1);

            var firstPage = await reader1.ReadNextPageAsync(context1);
            var firstHandles = firstPage.Cast<ShopifyProductLine>().Select(x => x.Handle).ToArray();
            Assert.Equal(2, firstPage.Length);
            Assert.Equal(new[] { "h1", "h2" }, firstHandles);

            var serialized = ((IResumableImportDataReader)reader1).GetSerializedCursor(context1);
            Assert.False(string.IsNullOrEmpty(serialized));

            // Act — phase 2: fresh reader on fresh stream, restore cursor, read next page
            var context2 = new ImportContext(profile);
            await using var stream2 = File.OpenRead(filePath);
            using var reader2 = new ShopifyProductDataReader(stream2, context2);

            var ok = ((IResumableImportDataReader)reader2).TryRestoreFromSerializedCursor(context2, serialized);
            Assert.True(ok);

            var secondPage = await reader2.ReadNextPageAsync(context2);
            var handles = secondPage.Cast<ShopifyProductLine>().Select(x => x.Handle).ToArray();

            // Assert
            Assert.Equal(new[] { "h3", "h4" }, handles);
        }

        private static ImportProfile CreateImportProfile(string fileName)
        {
            return new ImportProfile
            {
                DataImporterType = nameof(ShopifyProductImporter),
                ImportFileUrl = fileName,
                Settings = new List<ObjectSettingEntry>
                {
                    new() { Name = "Import.Csv.Delimiter", Value = ";" },
                    new() { Name = "Import.Csv.PageSize", Value = 2 },
                },
            };
        }
    }
}
