using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.MarketplaceVendorModule.Core.Common;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.MarketplaceVendorModule.Data.Commands;
using VirtoCommerce.MarketplaceVendorModule.Data.Queries;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.ImportModule.Data.Services;

namespace VirtoCommerce.ImportModule.Data.Importers
{
    public sealed class CsvOfferWriter : IImportDataWriter
    {
        private readonly IMediator _mediator;
        private readonly IOffersSearchService _offersSearchService;

        public CsvOfferWriter(IMediator mediator, IOffersSearchService offersSearchService)
        {
            _mediator = mediator;
            _offersSearchService = offersSearchService;
        }

        public async Task WriteAsync(object[] items, ImportContext context)
        {
            var validator = ExType<CsvOfferValidator>.New();
            var index = 0;

            var searchResult = await _offersSearchService.SearchAsync(new SearchOffersQuery
            {
                OuterIds = items.Cast<OfferDetails>().Select(x => x.OuterId).ToArray()
            });

            var searchResultOuterId = searchResult.Results.Select(x => x.OuterId);

            foreach (var offerDetails in items.Cast<OfferDetails>())
            {
                var validationResult = await validator.ValidateAsync(offerDetails);

                if (validationResult.IsValid)
                {
                    try
                    {
                        if (!searchResultOuterId.Contains(offerDetails.OuterId))
                        {
                            var createOfferCommand = new CreateNewOfferCommand
                            {
                                Details = offerDetails,
                                SellerId = context.ImportProfile?.SellerId,
                                SellerName = context.ImportProfile?.SellerName,
                                ProductId = offerDetails.ProductId
                            };
                            await _mediator.Send(createOfferCommand);
                        }
                        else
                        {
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
        }
    }
}
