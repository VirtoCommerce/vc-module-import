using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.CsvHelper;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public sealed class CsvProductImageImporter : IDataImporter, IFileBased
    {
        private readonly IBlobStorageProvider _blobStorageProvider;
        public CsvProductImageImporter(IBlobStorageProvider blobStorageProvider)
        {
            _blobStorageProvider = blobStorageProvider;
            Metadata = new Dictionary<string, string>()
            {
                { "availableFileExtensions", AvailableFileExtensions }
            };
        }
        /// <summary>
        /// Descrimiator
        /// </summary>
        public string TypeName { get; } = nameof(CsvProductImageImporter);

        /// <summary>
        /// Uses to pass some extra data fror importer to outside 
        /// </summary>
        public Dictionary<string, string> Metadata { get; private set; }

        /// <summary>
        /// Avail settings that importer exposes and allows to edit by users
        /// </summary>
        public SettingDescriptor[] AvailSettings { get; set; }

        public IAuthorizationRequirement AuthorizationReqirement { get; set; }

        public string AvailableFileExtensions => ".csv";

        public IImportDataReader OpenReader(ImportContext context)
        {
            if (string.IsNullOrEmpty(context.ImportProfile.ImportFileUrl))
            {
                throw new OperationCanceledException($"Import file must be set");
            }
            var importStream = _blobStorageProvider.OpenRead(context.ImportProfile.ImportFileUrl);

            return new CsvDataReader<ProductImage, CsvProductImageClassMap>(importStream, context);
        }

        public IImportDataWriter OpenWriter(ImportContext context)
        {
            return new CsvProductImageWriter(context);
        }
        public object Clone()
        {
            var result = MemberwiseClone() as CsvProductImageImporter;
            return result;
        }

        public async Task<ValidationResult> ValidateAsync(ImportContext context)
        {
            var result = new ValidationResult();
            if (string.IsNullOrEmpty(context.ImportProfile.ImportFileUrl))
            {
                result.Errors.Add("Import file must be set");
                return result;
            }

            var importStream = _blobStorageProvider.OpenRead(context.ImportProfile.ImportFileUrl);
            if (importStream.Length == 0)
            {
                result.Errors.Add("Import file must not empty");
                return result;
            }

            var reader = new CsvDataReader<ProductImage, CsvProductImageClassMap>(importStream, context);

            var productImageValidator = ExType<CsvProductImageValidator>.New();
            int lineNumber = 0;

            do
            {
                var items = await reader.ReadNextPageAsync(context);
                var productImages = items.Cast<ProductImage>();
                foreach (var productImage in productImages)
                {
                    lineNumber++;
                    var validationResult = await productImageValidator.ValidateAsync(productImage);
                    if (!validationResult.IsValid)
                    {
                        result.Errors.Add($"{validationResult} in line {lineNumber}");
                    }
                }
            } while (reader.HasMoreResults);

            return result;
        }
    }
}
