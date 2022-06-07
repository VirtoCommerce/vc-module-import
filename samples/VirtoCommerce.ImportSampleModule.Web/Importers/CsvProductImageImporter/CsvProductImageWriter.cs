using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public sealed class CsvProductImageWriter : IImportDataWriter
    {
        private readonly bool _debug;
        public CsvProductImageWriter(ImportContext context)
        {
            _debug = Convert.ToBoolean(context.ImportProfile.Settings.FirstOrDefault(x => x.Name == ProductImageImporterSettings.DebugSetting.Name)?.Value ?? false);
        }
        public Task WriteAsync(object[] items, ImportContext context)
        {
            var index = 0;
            try
            {
                foreach (var image in items.OfType<ProductImage>())
                {
                    var line = context.ProgressInfo.ProcessedCount + index;
                    //TODO: Add code for adding image to product
                    if (_debug)
                    {
                        Debug.WriteLine($"Line {line}: {image.ImageUrl} is added to product #{image.ProductId}");
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
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            //nothing to dispose
        }
    }
}
