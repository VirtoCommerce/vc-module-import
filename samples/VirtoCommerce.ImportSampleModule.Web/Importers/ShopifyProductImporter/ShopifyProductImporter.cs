using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.CsvHelper;
using VirtoCommerce.ImportSampleModule.Web.Search;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class ShopifyProductImporter : IDataImporter, IFileBased
    {
        private readonly IBlobStorageProvider _blobStorageProvider;
        private readonly IExtendedProductSearchService _productsSearchService;
        private readonly IItemService _itemService;

        public ShopifyProductImporter(IBlobStorageProvider blobStorageProvider,
            IExtendedProductSearchService productsSearchService,
            IItemService itemService
            )
        {
            _blobStorageProvider = blobStorageProvider;
            _productsSearchService = productsSearchService;
            _itemService = itemService;
            Metadata = new Dictionary<string, string>()
            {
                { "availableFileExtensions", AvailableFileExtensions }
            };
        }

        public string TypeName { get; } = nameof(ShopifyProductImporter);

        public Dictionary<string, string> Metadata { get; private set; }

        public SettingDescriptor[] AvailSettings { get; set; }

        public IAuthorizationRequirement AuthorizationRequirement { get; set; }

        public string AvailableFileExtensions => ".csv";

        public IImportDataReader OpenReader(ImportContext context)
        {
            if (string.IsNullOrEmpty(context.ImportProfile.ImportFileUrl))
            {
                throw new OperationCanceledException($"Import file must be set");
            }
            var importStream = _blobStorageProvider.OpenRead(context.ImportProfile.ImportFileUrl);

            return new CsvDataReader<ShopifyProductLine, ShopifyProductClassMap>(importStream, context);
        }

        public IImportDataWriter OpenWriter(ImportContext context)
        {
            return new ShopifyProductWriter(context, _productsSearchService, _itemService);
        }

        public async Task<ValidationResult> ValidateAsync(ImportContext context)
        {
            var result = new ValidationResult();
            if (string.IsNullOrEmpty(context.ImportProfile.ImportFileUrl))
            {
                result.Errors.Add("Import file must be set");
                return result;
            }

            try
            {
                var importStream = _blobStorageProvider.OpenRead(context.ImportProfile.ImportFileUrl);
                if (importStream.Length == 0)
                {
                    result.Errors.Add("Import file must not be empty");
                    return result;
                }

                var reader = new CsvDataReader<ShopifyProductLine, ShopifyProductClassMap>(importStream, context);

                var productValidator = ExType<ShopifyProductValidator>.New();
                int lineNumber = 0;

                do
                {
                    var items = await reader.ReadNextPageAsync(context);
                    var shopifyProducts = items.Cast<ShopifyProductLine>();
                    foreach (var shopifyProduct in shopifyProducts)
                    {
                        lineNumber++;
                        var validationResult = await productValidator.ValidateAsync(shopifyProduct);
                        if (!validationResult.IsValid)
                        {
                            result.Errors.Add($"{validationResult} in line {lineNumber}");
                        }
                    }
                } while (reader.HasMoreResults);
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
            }
            return result;
        }

        public object Clone()
        {
            var result = MemberwiseClone() as ShopifyProductImporter;
            return result;
        }
    }
}
