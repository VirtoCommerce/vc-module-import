using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Moq;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.ImportModule.Data.Importers.ShopifyProductImporter;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.ImportModule.Tests.Unit;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.MarketplaceVendorModule.Data.Commands;
using VirtoCommerce.MarketplaceVendorModule.Data.Queries;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Functional
{
    public class ShopifyProductImporterTest
    {
        private IBlobStorageProvider BlobStorageProvider => new BlobStorageProvider();

        [Theory]
        [MemberData(nameof(Input))]
        public async Task ShopifyProductImport_Flow(string shopifyFileName, SellerProduct[] sellerProducts, int linesToWriteCount)
        {
            // Arrange
            Mock<IMediator> Mediator = new Mock<IMediator>();
            foreach (var sellerProduct in sellerProducts)
            {
                Mediator.Setup(a => a.Send(It.Is<CreateNewProductCommand>(x => x.ProductDetails.OuterId == sellerProduct.ProductData.OuterId), It.IsAny<CancellationToken>())).ReturnsAsync(sellerProduct);
            }
            Mediator.Setup(a => a.Send(It.IsAny<CreateNewOfferCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Offer());

            Mock<ISellerProductsSearchService> SellerProductsSearchService = new Mock<ISellerProductsSearchService>();
            SellerProductsSearchService.Setup(a => a.SearchAsync(It.IsAny<SearchProductsQuery>())).ReturnsAsync(new SearchProductsResult());

            var importProfile = CreateImportProfile(shopifyFileName);

            var context = new ImportContext(importProfile);

            var dataImporter = new ShopifyProductImporter(BlobStorageProvider, Mediator.Object, SellerProductsSearchService.Object);

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

            Mediator.Verify(x => x.Send(It.IsAny<CreateNewProductCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(linesToWriteCount));
            Mediator.Verify(x => x.Send(It.IsAny<CreateNewOfferCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(linesToWriteCount));
        }

        public static TheoryData<string, SellerProduct[], int> Input()
        {
            return new TheoryData<string, SellerProduct[], int>()
            {
                // valid products
                {
                    "valid_shopify_products.csv",
                    new SellerProduct[]
                    {
                        new SellerProduct()
                        {
                            OuterId = "OuterId01",
                            StagedProductData =new CatalogProduct() {
                              Id = "ProductId01",
                              Code = "ProductSku01",
                              OuterId = "OuterId01"
                            }
                        },
                        new SellerProduct()
                        {
                            OuterId = "OuterId02",
                            StagedProductData =new CatalogProduct() {
                              Id = "ProductId02",
                              Code = "ProductSku02",
                              OuterId = "OuterId02"
                            }
                        }
                    },
                    2
                },
                // invalid products
                {
                    "invalid_shopify_products.csv",
                    new SellerProduct[0],
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
                        Name = "Vcmp.Import.Csv.Delimiter",
                        Value = ","
                    }
                }
            };
        }

    }
}
