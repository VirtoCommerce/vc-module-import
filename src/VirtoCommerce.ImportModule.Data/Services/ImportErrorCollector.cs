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
        internal const string LimitReachedMessage = "The import process has been canceled because it exceeds the configured maximum errors limit";

        private readonly int _threshold;
        private readonly int _capacity;
        private readonly ImportProgressInfo _progress;
        private readonly ILogger _logger;
        private FixedSizeQueue<ErrorInfo> _queue;

        public ImportErrorCollector(int threshold, ImportProgressInfo progress, ILogger logger)
        {
            _threshold = threshold;
            _capacity = Math.Max(threshold, MinQueueCapacity);
            _queue = new FixedSizeQueue<ErrorInfo>(_capacity);
            _progress = progress;
            _logger = logger;
        }

        // Errors raised by this run only: entries seeded from a resumed run's history never count toward the threshold,
        // so each resume gets a fresh budget.
        public int Count { get; private set; }

        public bool LimitReached => Count >= _threshold;

        public List<ErrorInfo> GetTopErrors() => _queue.GetTopValues().ToList();

        public void Seed(IEnumerable<string> storedErrors)
        {
            // Stored newest first: enqueue oldest first so the rendered order reproduces the stored one.
            // Keep Reverse() on IEnumerable<T>: on a List<T> it would bind to the in-place void overload.
            foreach (var error in (storedErrors ?? []).Where(x => x != LimitReachedMessage).Reverse())
            {
                _queue.Add(new StoredErrorInfo(error));
            }

            Render();
        }

        public void RemoveSeeded()
        {
            var kept = _queue.GetTopValues().Where(x => x is not StoredErrorInfo).Reverse().ToList();
            _queue = new FixedSizeQueue<ErrorInfo>(_capacity);
            foreach (var error in kept)
            {
                _queue.Add(error);
            }

            Render();
        }

        // Note: collected errors are surfaced to the client via the regular
        // progressCallback already invoked at each loop iteration / in the finally
        // block of ImportAsync, so this method does not push notifications itself.
        public void Handle(ErrorInfo info)
        {
            Count++;
            _queue.Add(info);
            _logger.LogError("{ErrorInfo}", info);
            Render();
            if (Count == _threshold)
            {
                _logger.LogError(LimitReachedMessage);
            }
        }

        private void Render()
        {
            var errors = _queue.GetTopValues().Select(x => x.ToString()).ToList();
            if (Count > 0 && LimitReached)
            {
                errors.Add(LimitReachedMessage);
            }

            _progress.Errors = errors;
        }

        // A stored string already carries its "Line N:" prefix; wrapping it in a new ErrorInfo would prefix it again.
        private sealed class StoredErrorInfo : ErrorInfo
        {
            public StoredErrorInfo(string error)
            {
                ErrorMessage = error;
            }

            public override string ToString() => ErrorMessage;
        }
    }
}
