using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.ImportModule.Data.Importers;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Data.Importers
{
    public sealed class CsvProductImageImporter : IDataImporter
    {
        private readonly IBlobStorageProvider _blobStorageProvider;
        public CsvProductImageImporter(IBlobStorageProvider blobStorageProvider)
        {
            _blobStorageProvider = blobStorageProvider;
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

    }
}
