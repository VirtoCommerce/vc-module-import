using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Moq;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Importers;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains.SellerProductAggregate;
using VirtoCommerce.MarketplaceVendorModule.Core.Queries;
using VirtoCommerce.MarketplaceVendorModule.Data.Commands;
using VirtoCommerce.MarketplaceVendorModule.Data.Queries;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class CsvProductImporterTests
    {
        private readonly IBlobStorageProvider _blobStorageProvider = new BlobStorageProvider();
        private readonly Mock<IMediator> _mediator = new();
        private readonly Mock<ISellerProductsSearchService> _sellerProductsSearchService = new();
        private readonly Mock<IPropertyDictionaryItemService> _propDictItemService = new();
        private readonly Mock<IPropertyDictionaryItemSearchService> _propDictItemSearchService = new();
        private readonly PropertyMetadataLoader _propertyMetadataLoader;

        public CsvProductImporterTests()
        {
            _propertyMetadataLoader = new PropertyMetadataLoader(_propDictItemService.Object, _propDictItemSearchService.Object);
            _mediator.Setup(a => a.Send(It.IsAny<SearchCategoriesQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(new CategorySearchResult());
            _mediator.Setup(a => a.Send(It.IsAny<CreateNewProductCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(new SellerProduct());
        }

        [Fact]
        public async Task Read_full_product_graph_from_valid_csv_product_is_readed()
        {
            // Arrange
            var importProfile = new ImportProfile
            {
                DataImporterType = nameof(CsvProductImporter),
                ImportFileUrl = TestHepler.GetFilePath("valid_csv_product_is_readed.csv"),
                Settings = new List<ObjectSettingEntry>()
            };
            var context = new ImportContext(importProfile);

            var dataImporter = new CsvProductImporter(_blobStorageProvider,
                _mediator.Object,
                _sellerProductsSearchService.Object,
                _propertyMetadataLoader);

            using var reader = dataImporter.OpenReader(context);

            // Act
            var totalCount = await reader.GetTotalCountAsync(context);
            var items = await reader.ReadNextPageAsync(context);
            var products = items.Cast<ProductDetails>();

            // Assertion
            totalCount.Should().Be(1);
            products.FirstOrDefault(x => x.Name == "Decsription").Should().BeNull();
            products.FirstOrDefault().Properties.Count.Should().Be(6);
        }

        [Fact]
        public void Read_full_product_graph_from_invalid_csv_error_is_generated()
        {
            // Arrange
            var importProfile = new ImportProfile
            {
                DataImporterType = nameof(CsvProductImporter),
                Settings = new List<ObjectSettingEntry>()
            };
            var context = new ImportContext(importProfile);

            var dataImporter = new CsvProductImporter(_blobStorageProvider,
                _mediator.Object,
                _sellerProductsSearchService.Object,
                _propertyMetadataLoader);

            // Act
            try
            {
                using var reader = dataImporter.OpenReader(context);
            }

            // Assertion
            catch (Exception ex)
            {
                ex.Message.Should().Be("Import file must be set");
            }
        }

        [Fact]
        public async Task Import_new_product_from_csv_product_is_created()
        {
            // Arrange
            var importProfile = new ImportProfile
            {
                DataImporterType = nameof(CsvProductImporter),
                Settings = new List<ObjectSettingEntry>()
            };
            var context = new ImportContext(importProfile);

            _sellerProductsSearchService.Setup(a => a.SearchAsync(It.IsAny<SearchProductsQuery>())).ReturnsAsync(new SearchProductsResult());
            var dataImporter = new CsvProductImporter(_blobStorageProvider,
                _mediator.Object,
                _sellerProductsSearchService.Object,
                _propertyMetadataLoader);

            var items = GetProductDetails();
            using var writer = dataImporter.OpenWriter(context);


            // Act
            await writer.WriteAsync(items, context);

            // Assertion
            _mediator.Verify(x => x.Send(It.IsAny<CreateNewProductCommand>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Import_exist_product_from_csv_product_isnt_updated()
        {
            // Arrange
            var importProfile = new ImportProfile
            {
                DataImporterType = nameof(CsvProductImporter),
                Settings = new List<ObjectSettingEntry>()
            };
            var context = new ImportContext(importProfile);

            _sellerProductsSearchService.Setup(a => a.SearchAsync(It.IsAny<SearchProductsQuery>())).ReturnsAsync(new SearchProductsResult()
            {
                Results = new List<SellerProduct>()
                {
                    new SellerProduct()
                    {
                        OuterId = "TestOuterId"
                    }
                }
            });
            var dataImporter = new CsvProductImporter(_blobStorageProvider,
                _mediator.Object,
                _sellerProductsSearchService.Object,
                _propertyMetadataLoader);

            var items = GetProductDetails();
            using var writer = dataImporter.OpenWriter(context);


            // Act
            await writer.WriteAsync(items, context);

            // Assertion
            _mediator.Verify(x => x.Send(It.IsAny<CreateNewProductCommand>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task Import_product_with_properties_properties_metadata_resoved()
        {
            // Arrange
            var propertyValue = new PropertyValue()
            {
                PropertyName = "Dictionary",
            };
            var allCategoryProperties = new Property[]
            {
                new Property()
                {
                    Name = "Dictionary",
                    Dictionary = true,
                    ValueType = PropertyValueType.ShortText,
                }
            };
            _propDictItemSearchService.Setup(a => a.SearchAsync(It.IsAny<PropertyDictionaryItemSearchCriteria>())).ReturnsAsync(new PropertyDictionaryItemSearchResult()
            {
                Results = new PropertyDictionaryItem[]
                {
                    new PropertyDictionaryItem()
                    {
                        Alias = "AliasTest"
                    }
                }
            });

            // Act
            await _propertyMetadataLoader.TryLoadMetadata(propertyValue, allCategoryProperties, false);

            // Assertion
            propertyValue.ValueType.Should().Be(PropertyValueType.ShortText);
            propertyValue.Alias.Should().Be("AliasTest");
        }

        private static object[] GetProductDetails()
        {
            return new object[] { new ProductDetails()
            {
                CategoryId = "TestCategoryId",
                OuterId = "TestOuterId",
            }};
        }
    }
}
