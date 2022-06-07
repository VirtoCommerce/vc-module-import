using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportSampleModule.Web.Search;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class ShopifyProductWriter : IImportDataWriter
    {
        private readonly bool _debug;
        private readonly string _defaultCategoryId;
        private readonly IExtendedProductSearchService _productsSearchService;
        private readonly IItemService _itemService;

        public ShopifyProductWriter(ImportContext context,
            IExtendedProductSearchService productsSearchService,
            IItemService itemService
            )
        {
            _debug = Convert.ToBoolean(context.ImportProfile.Settings.FirstOrDefault(x => x.Name == ShopifyProductSettings.DebugSetting.Name)?.Value ?? false);
            _defaultCategoryId = context.ImportProfile.Settings.FirstOrDefault(x => x.Name == ShopifyProductSettings.CategoryIdSetting.Name)?.Value?.ToString();

            _productsSearchService = productsSearchService;
            _itemService = itemService;
        }

        public async Task WriteAsync(object[] items, ImportContext context)
        {
            var index = 0;
            var productValidator = ExType<ShopifyProductValidator>.New();

            try
            {
                var shopifyProducts = items.Cast<ShopifyProductLine>();

                var searchResult = await _productsSearchService.SearchAsync(new ExtendedProductSearchCriteria
                {
                    OuterIds = shopifyProducts.Select(x => x.Handle).Distinct().ToArray()
                });

                var existedOuterIds = searchResult.Results.Select(x => x.OuterId);

                foreach (var shopifyProduct in shopifyProducts)
                {
                    var validationResult = await productValidator.ValidateAsync(shopifyProduct);

                    if (validationResult.IsValid)
                    {
                        if (_debug)
                        {
                            int line = context.ProgressInfo.ProcessedCount + index;
                            Debug.WriteLine($"Line {line}: {shopifyProduct.Title} is added to product #{shopifyProduct.Handle}");
                        }
                        else
                        {
                            if (!existedOuterIds.Contains(shopifyProduct.Handle))
                            {
                                var catalogProduct = shopifyProduct.ToProductDetails();
                                catalogProduct.CategoryId = _defaultCategoryId;

                                await _itemService.SaveChangesAsync(new[] { catalogProduct });
                            }
                            else
                            {
                                // do something?
                            }
                        }
                    }

                    index++;
                }
            }
            catch (Exception ex)
            {
                var errorInfo = new ErrorInfo
                {
                    ErrorLine = context.ProgressInfo.ProcessedCount + index,
                    ErrorMessage = ex.Message,
                };
                context.ErrorCallback(errorInfo);
            }
        }

        public void Dispose()
        {
        }
    }
}
