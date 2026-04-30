using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Data.Services
{
    internal sealed class ImportErrorCollector
    {
        private readonly int _threshold;
        private readonly FixedSizeQueue<ErrorInfo> _queue;
        private readonly ImportProgressInfo _progress;
        private readonly Func<ImportProgressInfo, Task> _progressCallback;
        private readonly ILogger _logger;

        public ImportErrorCollector(
            int threshold,
            ImportProgressInfo progress,
            Func<ImportProgressInfo, Task> progressCallback,
            ILogger logger)
        {
            _threshold = threshold;
            _queue = new FixedSizeQueue<ErrorInfo>(Math.Max(threshold, 50));
            _progress = progress;
            _progressCallback = progressCallback;
            _logger = logger;
        }

        public int Count { get; private set; }

        public bool LimitReached => Count >= _threshold;

        public List<ErrorInfo> GetTopErrors() => _queue.GetTopValues().ToList();

        public void Handle(ErrorInfo info)
        {
            Count++;
            _queue.Add(info);
            _logger.LogError(info.ToString());
            _progress.Errors = _queue.GetTopValues().Select(x => x.ToString()).ToList();
            if (Count == _threshold)
            {
                const string limitErrorMessage = "The import process has been canceled because it exceeds the configured maximum errors limit";
                _progress.Errors.Add(limitErrorMessage);
                _logger.LogError(limitErrorMessage);
            }
            _progressCallback(_progress).GetAwaiter().GetResult();
        }
    }
}
