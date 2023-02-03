using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.CsvHelper
{
    public class CsvDataReporter : IImportReporter
    {
        private readonly IBlobStorageProvider _blobStorageProvider;

        private readonly string _errorColumnName = "Error";
        private string _reportUrl;
        private string _delimiter;

        public string ReportUrl => _reportUrl;

        public CsvDataReporter(IBlobStorageProvider blobStorageProvider)
        {
            _blobStorageProvider = blobStorageProvider;
        }

        public void SetContext(ImportProfile importProfile)
        {
            if (string.IsNullOrEmpty(importProfile.ImportReportUrl))
            {
                importProfile.ImportReportUrl = GetDefaultReportFilePath(importProfile.ImportFileUrl);
            }
            _reportUrl = importProfile.ImportReportUrl;

            _delimiter = importProfile.Settings.GetSettingValue(CsvSettings.Delimiter.Name, (string)CsvSettings.Delimiter.DefaultValue);
        }

        public async Task<string> SaveErrorsAsync(List<ErrorInfo> errorsToSave)
        {
            using Stream reportStream = _blobStorageProvider.OpenWrite(_reportUrl);
            using StreamWriter streamWriter = new StreamWriter(reportStream);

            string originalDataHeader = errorsToSave.FirstOrDefault(x => !string.IsNullOrEmpty(x.RawHeader))?.RawHeader;
            if (!string.IsNullOrEmpty(originalDataHeader))
            {
                await streamWriter.WriteLineAsync($"{_errorColumnName}{_delimiter}{originalDataHeader}");
            }

            foreach (ErrorInfo errorInfo in errorsToSave)
            {
                await streamWriter.WriteLineAsync($"{errorInfo.ErrorMessage}{_delimiter}{errorInfo.RawData}");
            }

            return _reportUrl;
        }

        public void Dispose()
        {
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        private string GetDefaultReportFilePath(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var fileExtension = Path.GetExtension(fileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var reportFileName = $"{fileNameWithoutExtension}_report{fileExtension}";
            var result = filePath.Replace(fileName, reportFileName);

            return result;
        }
    }
}
