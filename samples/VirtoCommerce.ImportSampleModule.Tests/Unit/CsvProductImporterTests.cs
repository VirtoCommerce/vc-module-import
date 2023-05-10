using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.CsvHelper.Services;
using VirtoCommerce.ImportSampleModule.Web.Importers;
using VirtoCommerce.ImportSampleModule.Web.Search;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.ImportSampleModule.Tests.Unit
{
    public class CsvProductImporterTests
    {
        private readonly IBlobStorageProvider _blobStorageProvider = new BlobStorageProvider();
        private readonly Mock<IExtendedProductSearchService> _extendedProductSearchService = new();
        private readonly Mock<IPropertyDictionaryItemService> _propDictItemService = new();
        private readonly Mock<IPropertyDictionaryItemSearchService> _propDictItemSearchService = new();
        private readonly PropertyMetadataLoader _propertyMetadataLoader;
        private readonly Mock<IItemService> _itemService = new();
        private readonly Mock<ICategoryService> _categoryService = new();
        private readonly Mock<IServiceProvider> _serviceProvider = new();
        private readonly Mock<ClassMapBuilder<CsvProductClassMap, CatalogProduct>> _classMapBuilder = new();
        private readonly Mock<ClassMapRegistrar<CsvProductClassMap, CatalogProduct>> _classMapRegistrar = new();
        private readonly Mock<CsvProductClassMap> _csvProductClassMap = new();

        public CsvProductImporterTests()
        {
            _propertyMetadataLoader = new PropertyMetadataLoader(_propDictItemService.Object, _propDictItemSearchService.Object);
            _classMapBuilder.Setup(x => x.WithSettings(It.IsAny<ICollection<ObjectSettingEntry>>())).Returns(() => _classMapBuilder.Object);
            _classMapRegistrar.Setup(x => x.Register(It.IsAny<Func<CsvProductClassMap>>())).Returns(() => _classMapBuilder.Object);
            _serviceProvider.Setup(x => x.GetService(typeof(ClassMapRegistrar<CsvProductClassMap, CatalogProduct>))).Returns(() => _classMapRegistrar.Object);
            _serviceProvider.Setup(x => x.GetService(typeof(CsvProductClassMap))).Returns(() => _csvProductClassMap.Object);
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
                _extendedProductSearchService.Object,
                _propertyMetadataLoader,
                _itemService.Object,
                _categoryService.Object,
                _serviceProvider.Object);

            using var reader = dataImporter.OpenReader(context);

            // Act
            var totalCount = await reader.GetTotalCountAsync(context);
            var items = await reader.ReadNextPageAsync(context);
            var products = items.Cast<CatalogProduct>();

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
                _extendedProductSearchService.Object,
                _propertyMetadataLoader,
                _itemService.Object,
                _categoryService.Object,
                _serviceProvider.Object);

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

            _extendedProductSearchService.Setup(a => a.SearchAsync(It.IsAny<ExtendedProductSearchCriteria>())).ReturnsAsync(new ProductSearchResult());
            var dataImporter = new CsvProductImporter(_blobStorageProvider,
                _extendedProductSearchService.Object,
                _propertyMetadataLoader,
                _itemService.Object,
                _categoryService.Object,
                _serviceProvider.Object);

            var items = GetProducts();
            using var writer = dataImporter.OpenWriter(context);

            // Act
            await writer.WriteAsync(items, context);

            // Assertion
            _itemService.Verify(x => x.SaveChangesAsync(It.IsAny<CatalogProduct[]>()), Times.Once());
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

            _extendedProductSearchService.Setup(a => a.SearchAsync(It.IsAny<ExtendedProductSearchCriteria>())).ReturnsAsync(new ProductSearchResult()
            {
                Results = new List<CatalogProduct>()
                {
                    new CatalogProduct()
                    {
                        OuterId = "TestOuterId"
                    }
                }
            });
            var dataImporter = new CsvProductImporter(_blobStorageProvider,
                _extendedProductSearchService.Object,
                _propertyMetadataLoader,
                _itemService.Object,
                _categoryService.Object,
                _serviceProvider.Object);

            var items = GetProducts();
            using var writer = dataImporter.OpenWriter(context);


            // Act
            await writer.WriteAsync(items, context);

            // Assertion
            _itemService.Verify(x => x.SaveChangesAsync(It.IsAny<CatalogProduct[]>()), Times.Never());
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

        private static object[] GetProducts()
        {
            return new object[] { new CatalogProduct()
            {
                CategoryId = "TestCategoryId",
                OuterId = "TestOuterId",
            }};
        }
    }
}
