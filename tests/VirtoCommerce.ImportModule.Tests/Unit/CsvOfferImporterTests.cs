using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Moq;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.ImportModule.Data.Importers;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.MarketplaceVendorModule.Core.Domains;
using VirtoCommerce.MarketplaceVendorModule.Data.Commands;
using VirtoCommerce.MarketplaceVendorModule.Data.Queries;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class CsvOfferImporterTests
    {
        private readonly IBlobStorageProvider _blobStorageProvider = new BlobStorageProvider();
        private readonly Mock<IMediator> _mediator = new();
        private readonly Mock<IOffersSearchService> _offersSearchService = new();

        [Fact]
        public async Task Read_full_offer_graph_from_valid_csv_offer_is_readed()
        {
            // Arrange
            var importProfile = new ImportProfile
            {
                DataImporterType = nameof(CsvOfferImporter),
                ImportFileUrl = TestHepler.GetFilePath("valid_csv_offer_is_readed.csv"),
                Settings = new List<ObjectSettingEntry>()
            };
            var context = new ImportContext(importProfile);

            var dataImporter = new CsvOfferImporter(_blobStorageProvider,
                _mediator.Object,
                _offersSearchService.Object);

            using var reader = dataImporter.OpenReader(context);

            // Act
            var totalCount = await reader.GetTotalCountAsync(context);
            var items = await reader.ReadNextPageAsync(context);
            var offers = items.Cast<OfferDetails>();

            // Assertion
            totalCount.Should().Be(1);
            offers.FirstOrDefault(x => x.Name == "Decsription").Should().BeNull();
            offers.FirstOrDefault().Prices.Count.Should().Be(3);
        }

        [Theory]
        [InlineData("../../../Unit/data/invalid_csv_offer_is_readed.csv", "Header with name 'Currency'[0] was not found")]
        [InlineData(null, "Import file must be set")]
        public async Task Read_full_offer_graph_from_invalid_csv_error_is_generated(string input, string expected)
        {
            // Arrange
            var importProfile = new ImportProfile
            {
                DataImporterType = nameof(CsvOfferImporter),
                ImportFileUrl = input,
                Settings = new List<ObjectSettingEntry>()
            };
            var context = new ImportContext(importProfile);

            var dataImporter = new CsvOfferImporter(_blobStorageProvider,
                _mediator.Object,
                _offersSearchService.Object);

            // Act
            try
            {
                using var reader = dataImporter.OpenReader(context);
                var items = await reader.ReadNextPageAsync(context);
            }

            // Assertion
            catch (Exception ex)
            {
                ex.Message.Should().Contain(expected);
            }
        }

        [Theory]
        [InlineData("NewOuterId", 1)]
        [InlineData("ExistingOuterId", 0)]
        public async Task Import_new_offer_from_csv_offer_is_created_or_isnt_updated(string input, int expected)
        {
            // Arrange
            var importProfile = new ImportProfile
            {
                DataImporterType = nameof(CsvOfferImporter),
                Settings = new List<ObjectSettingEntry>()
            };
            var context = new ImportContext(importProfile);

            _offersSearchService.Setup(a => a.SearchAsync(It.IsAny<SearchOffersQuery>())).ReturnsAsync(new SearchOffersResult()
            {
                Results = new List<Offer>()
                {
                    new Offer()
                    {
                        OuterId = input
                    }
                }
            });
            var dataImporter = new CsvOfferImporter(_blobStorageProvider,
                _mediator.Object,
                _offersSearchService.Object);

            var items = GetOfferDetails();
            using var writer = dataImporter.OpenWriter(context);


            // Act
            await writer.WriteAsync(items, context);

            // Assertion
            _mediator.Verify(x => x.Send(It.IsAny<CreateNewOfferCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(expected));
        }

        private static object[] GetOfferDetails()
        {
            return new object[] { new OfferDetails()
            {
                OuterId = "ExistingOuterId",
            }};
        }
    }
}
