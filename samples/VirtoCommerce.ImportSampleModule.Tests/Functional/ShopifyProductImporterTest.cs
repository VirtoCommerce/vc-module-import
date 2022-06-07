using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportSampleModule.Tests.Unit;
using VirtoCommerce.ImportSampleModule.Web.Importers;
using VirtoCommerce.ImportSampleModule.Web.Search;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.ImportSampleModule.Tests.Functional
{
    public class ShopifyProductImporterTest
    {
        private IBlobStorageProvider BlobStorageProvider => new BlobStorageProvider();

        [Theory]
        [MemberData(nameof(Input))]
        public async Task ShopifyProductImport_Flow(string shopifyFileName, int linesToWriteCount)
        {
            // Arrange
            Mock<IExtendedProductSearchService> ExtendedProductSearchService = new Mock<IExtendedProductSearchService>();
            ExtendedProductSearchService.Setup(a => a.SearchAsync(It.IsAny<ExtendedProductSearchCriteria>())).ReturnsAsync(new ProductSearchResult());

            Mock<IItemService> ItemService = new Mock<IItemService>();

            var importProfile = CreateImportProfile(shopifyFileName);

            var context = new ImportContext(importProfile);

            var dataImporter = new ShopifyProductImporter(BlobStorageProvider, ExtendedProductSearchService.Object, ItemService.Object);

            using var reader = dataImporter.OpenReader(context);

            // Act
            var totalCount = await reader.GetTotalCountAsync(context);
            var items = await reader.ReadNextPageAsync(context);
            var shopifyProducts = items.Cast<ShopifyProductLine>();

            using var writer = dataImporter.OpenWriter(context);
            await writer.WriteAsync(items, context);

            // Assertion
            totalCount.Should().Be(shopifyProducts.Count());

            foreach (var product in shopifyProducts)
            {
                product.Should().NotBeNull();
            }

            ItemService.Verify(x => x.SaveChangesAsync(It.IsAny<CatalogProduct[]>()), Times.Exactly(linesToWriteCount));
        }

        public static TheoryData<string, int> Input()
        {
            return new TheoryData<string, int>()
            {
                // valid products
                {
                    "valid_shopify_products.csv",
                    2
                },
                // invalid products
                {
                    "invalid_shopify_products.csv",
                    0
                }
            };
        }

        private ImportProfile CreateImportProfile(string fileName)
        {
            return new ImportProfile
            {
                DataImporterType = nameof(ShopifyProductImporter),
                ImportFileUrl = TestHepler.GetFilePath(fileName),
                Settings = new List<ObjectSettingEntry>()
                {
                    new ObjectSettingEntry()
                    {
                        Name = "Import.Csv.Delimiter",
                        Value = ","
                    }
                }
            };
        }

    }
}
