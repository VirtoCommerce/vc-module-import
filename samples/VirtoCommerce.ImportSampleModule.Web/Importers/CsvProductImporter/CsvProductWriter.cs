using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportSampleModule.Web.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class CsvProductWriter : IImportDataWriter
    {
        private readonly IExtendedProductSearchService _productsSearchService;
        private readonly PropertyMetadataLoader _propertyMetadataLoader;
        private readonly IItemService _itemService;
        private readonly ICategoryService _categoryService;

        public CsvProductWriter(IExtendedProductSearchService productsSearchService,
            PropertyMetadataLoader propertyMetadataResolver,
            IItemService itemService,
            ICategoryService categoryService
            )
        {
            _productsSearchService = productsSearchService;
            _propertyMetadataLoader = propertyMetadataResolver;
            _itemService = itemService;
            _categoryService = categoryService;
        }

        public async Task WriteAsync(object[] items, ImportContext context)
        {
            var products = items.Cast<CatalogProduct>().ToArray();

            //Load properties metadata for product before save
            await LoadPropertiesMetadata(products, context);

            var validator = ExType<CsvProductValidator>.New();
            var index = 0;

            var searchResult = await _productsSearchService.SearchAsync(new ExtendedProductSearchCriteria
            {
                OuterIds = products.Select(x => x.OuterId).ToArray()
            });
            var searchResultOuterIds = searchResult.Results.Select(x => x.OuterId).ToArray();

            foreach (var product in products)
            {
                var validationResult = await validator.ValidateAsync(product);

                if (validationResult.IsValid)
                {
                    try
                    {
                        if (!searchResultOuterIds.Contains(product.OuterId))
                        {
                            await _itemService.SaveChangesAsync(new[] { product });
                        }
                        else
                        {
                            //TODO: Move to ErrorDescriber
                            var errorInfo = new ErrorInfo
                            {
                                ErrorLine = context.ProgressInfo.ProcessedCount + index,
                                ErrorCode = "ImportError",
                                ErrorMessage = "This item already exists"
                            };
                            context.ErrorCallback(errorInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (context.ErrorCallback != null)
                        {
                            //TODO: Move to ErrorDescriber
                            var errorInfo = new ErrorInfo
                            {
                                ErrorLine = context.ProgressInfo.ProcessedCount + index,
                                ErrorCode = "ImportError",
                                ErrorMessage = ex.Message
                            };
                            context.ErrorCallback(errorInfo);
                        }
                    }
                }
                else
                {
                    if (context.ErrorCallback != null)
                    {
                        foreach (var validationFailure in validationResult.Errors)
                        {
                            var errorInfo = new ErrorInfo
                            {
                                ErrorLine = context.ProgressInfo.ProcessedCount + index,
                                ErrorCode = validationFailure.ErrorCode,
                                ErrorMessage = validationFailure.ErrorMessage,
                            };

                            context.ErrorCallback(errorInfo);
                        }
                    }
                }
                index++;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        private async Task LoadPropertiesMetadata(IList<CatalogProduct> products, ImportContext context)
        {
            var createNewDictItemIfNotFound = context.ImportProfile.Settings.GetValue<bool>(CsvProductSettings.CreateDictionaryValues);

            var categoriesIds = products.Select(x => x.CategoryId).Distinct().ToArray();
            var categories = await _categoryService.GetAsync(categoriesIds, null) ?? Array.Empty<Category>();
            var categoriesByIdDict = categories.ToDictionary(x => x.Id).WithDefaultValue(null);

            foreach (var product in products)
            {
                var allCategoryProperties = categoriesByIdDict[product.CategoryId]?.Properties?.ToArray();
                if (allCategoryProperties != null)
                {
                    foreach (var propertyValue in product.Properties?.SelectMany(x => x.Values) ?? Enumerable.Empty<PropertyValue>())
                    {
                        var property = await _propertyMetadataLoader.TryLoadMetadata(propertyValue, allCategoryProperties, createNewDictItemIfNotFound);
                        if (property != null && property.Dictionary && propertyValue.ValueId == null)
                        {
                            var errorInfo = new ErrorInfo
                            {
                                ErrorMessage = $"The '{propertyValue.Alias}' dictionary item is not found in '{propertyValue.PropertyName}' dictionary",
                                ErrorCode = "ImportError",
                            };
                            context.ErrorCallback(errorInfo);
                        }
                    }
                }
            }
        }
    }
}
