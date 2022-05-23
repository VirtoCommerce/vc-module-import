using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.MarketplaceVendorModule.Data.Commands;
using VirtoCommerce.MarketplaceVendorModule.Data.Queries;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.ImportModule.Data.Services;

namespace VirtoCommerce.ImportModule.Data.Importers.ShopifyProductImporter
{
    public class ShopifyProductWriter : IImportDataWriter
    {
        private readonly bool _debug;
        private readonly string _defaultCategoryId;
        private readonly string _defaultCurrency;
        private readonly IMediator _mediator;
        private readonly ISellerProductsSearchService _sellerProductsSearchService;

        public ShopifyProductWriter(ImportContext context,
            IMediator mediator,
            ISellerProductsSearchService sellerProductsSearchService
            )
        {
            _debug = Convert.ToBoolean(context.ImportProfile.Settings.FirstOrDefault(x => x.Name == ShopifyProductSettings.DebugSetting.Name)?.Value ?? false);
            _defaultCategoryId = context.ImportProfile.Settings.FirstOrDefault(x => x.Name == ShopifyProductSettings.CategoryIdSetting.Name)?.Value?.ToString();
            _defaultCurrency = context.ImportProfile.Settings.FirstOrDefault(x => x.Name == ShopifyProductSettings.CurrencySetting.Name)?.Value?.ToString();

            _mediator = mediator;
            _sellerProductsSearchService = sellerProductsSearchService;
        }

        public async Task WriteAsync(object[] items, ImportContext context)
        {
            var index = 0;
            var productValidator = ExType<ShopifyProductValidator>.New();

            try
            {
                var shopifyProducts = items.Cast<ShopifyProductLine>();

                var searchResult = await _sellerProductsSearchService.SearchAsync(new SearchProductsQuery
                {
                    OuterIds = shopifyProducts.Select(x => x.Handle).Distinct().ToArray()
                });

                var existedOuterIds = searchResult.Results.Select(x => x.OuterId);
                var existedProducts = searchResult.Results.ToDictionary(x => x.OuterId, x => x.ProductData);

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
                                var product = shopifyProduct.ToProductDetails();
                                product.CategoryId = _defaultCategoryId;
                                var createProductCommand = new CreateNewProductCommand
                                {
                                    ProductDetails = product,
                                    SellerId = context.ImportProfile?.SellerId,
                                    SellerName = context.ImportProfile?.SellerName,
                                };
                                var newSellerProduct = await _mediator.Send(createProductCommand);

                                existedOuterIds.Append(product.OuterId);
                                existedProducts.Add(newSellerProduct.OuterId, newSellerProduct.ProductData);

                                var offer = shopifyProduct.ToOfferDetails();
                                if (existedProducts.ContainsKey(offer.OuterId))
                                {
                                    var existedProduct = existedProducts[offer.OuterId];
                                    offer.ProductId = existedProduct.Id;
                                    offer.Sku = existedProduct.Code;
                                    offer.Currency = _defaultCurrency;

                                    var createOfferCommand = new CreateNewOfferCommand
                                    {
                                        Details = offer,
                                        SellerId = context.ImportProfile?.SellerId,
                                        SellerName = context.ImportProfile?.SellerName,
                                        ProductId = offer.ProductId,
                                        OuterId = offer.OuterId
                                    };
                                    await _mediator.Send(createOfferCommand);
                                }
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
