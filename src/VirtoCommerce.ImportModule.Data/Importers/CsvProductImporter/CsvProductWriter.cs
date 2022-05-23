using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains.SellerProductAggregate;
using VirtoCommerce.MarketplaceVendorModule.Core.Queries;
using VirtoCommerce.MarketplaceVendorModule.Data.Commands;
using VirtoCommerce.MarketplaceVendorModule.Data.Queries;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Data.Importers
{
    public class CsvProductWriter : IImportDataWriter
    {
        private readonly IMediator _mediator;
        private readonly ISellerProductsSearchService _sellerProductsSearchService;
        private readonly PropertyMetadataLoader _propertyMetadataLoader;

        public CsvProductWriter(IMediator mediator,
            ISellerProductsSearchService sellerProductsSearchService,
            PropertyMetadataLoader propertyMetadataResolver
            )
        {
            _mediator = mediator;
            _sellerProductsSearchService = sellerProductsSearchService;
            _propertyMetadataLoader = propertyMetadataResolver;
        }

        public async Task WriteAsync(object[] items, ImportContext context)
        {
            var products = items.Cast<ProductDetails>();

            //Load properties metadata for product before save
            await LoadPropertiesMetadata(products, context);

            var validator = ExType<CsvProductValidator>.New();
            var index = 0;

            var searchResult = await _sellerProductsSearchService.SearchAsync(new SearchProductsQuery
            {
                OuterIds = products.Select(x => x.OuterId).ToArray()
            });
            var searchResultOuterId = searchResult.Results.Select(x => x.OuterId);

            foreach (var productDetails in products)
            {
                var validationResult = await validator.ValidateAsync(productDetails);

                if (validationResult.IsValid)
                {
                    try
                    {
                        if (!searchResultOuterId.Contains(productDetails.OuterId))
                        {
                            var createProductCommand = new CreateNewProductCommand
                            {
                                ProductDetails = productDetails,
                                SellerId = context.ImportProfile?.SellerId,
                                SellerName = context.ImportProfile?.SellerName,
                            };
                            await _mediator.Send(createProductCommand);
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
        private async Task LoadPropertiesMetadata(IEnumerable<ProductDetails> products, ImportContext context)
        {
            var createNewDictItemIfNotFound = context.ImportProfile.Settings.GetSettingValue(CsvProductSettings.CreateDictionaryValues.Name, (bool)CsvProductSettings.CreateDictionaryValues.DefaultValue);

            var sellerCategoriesIds = products.Select(x => x.CategoryId).Distinct().ToArray();
            // Load all categories for imported products with their properties (it is important use this method since it loads categories properties from master catalog)
            var searchQuery = new SearchCategoriesQuery
            {
                ObjectIds = sellerCategoriesIds,
            };

            var result = await _mediator.Send(searchQuery);
            var categoriesByIdDict = result.Results.ToDictionary(x => x.Id).WithDefaultValue(null);

            foreach (var product in products)
            {
                var allCategoryProperties = categoriesByIdDict[product.CategoryId]?.Properties?.ToArray();
                if (allCategoryProperties != null)
                {
                    foreach (var propertyValue in product.Properties?.SelectMany(x => x.Values))
                    {
                        var property = await _propertyMetadataLoader.TryLoadMetadata(propertyValue, allCategoryProperties, createNewDictItemIfNotFound);
                        if (property != null && property.Dictionary && propertyValue.ValueId == null)
                        {
                            var errorInfo = new ErrorInfo
                            {
                                ErrorMessage = $"The '{propertyValue.Alias}' dictionary item is not found in '{propertyValue.PropertyName}' dictionary",
                                ErrorCode = "ImportError"
                            };
                            context.ErrorCallback(errorInfo);
                        }
                    }
                }
            }
        }
    }
}
