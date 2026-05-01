using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Data.Services
{
    internal sealed class ImportErrorCollector
    {
        private const int MinQueueCapacity = 50;
        private const string LimitReachedMessage = "The import process has been canceled because it exceeds the configured maximum errors limit";

        private readonly int _threshold;
        private readonly FixedSizeQueue<ErrorInfo> _queue;
        private readonly ImportProgressInfo _progress;
        private readonly ILogger _logger;

        public ImportErrorCollector(int threshold, ImportProgressInfo progress, ILogger logger)
        {
            _threshold = threshold;
            _queue = new FixedSizeQueue<ErrorInfo>(Math.Max(threshold, MinQueueCapacity));
            _progress = progress;
            _logger = logger;
        }

        public int Count { get; private set; }

        public bool LimitReached => Count >= _threshold;

        public List<ErrorInfo> GetTopErrors() => _queue.GetTopValues().ToList();

        // Note: collected errors are surfaced to the client via the regular
        // progressCallback already invoked at each loop iteration / in the finally
        // block of ImportAsync, so this method does not push notifications itself.
        public void Handle(ErrorInfo info)
        {
            Count++;
            _queue.Add(info);
            _logger.LogError("{ErrorInfo}", info);
            _progress.Errors = _queue.GetTopValues().Select(x => x.ToString()).ToList();
            if (Count == _threshold)
            {
                _progress.Errors.Add(LimitReachedMessage);
                _logger.LogError(LimitReachedMessage);
            }
        }
    }
}
