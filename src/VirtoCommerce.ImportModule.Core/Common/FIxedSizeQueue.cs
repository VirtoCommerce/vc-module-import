using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtoCommerce.ImportModule.Core.Common
{
    public class FixedSizeQueue<T>
    {
        private readonly Queue<T> collection;
        private readonly int maxSize;

        public FixedSizeQueue(int size)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than zero.");
            }

            collection = new Queue<T>(size);
            maxSize = size;
        }

        public int Count => collection.Count;

        public void Add(T item)
        {
            collection.Enqueue(item);

            if (collection.Count > maxSize)
            {
                collection.Dequeue();
            }
        }

        public IEnumerable<T> GetTopValues()
        {
            return collection.Reverse();
        }
    }
}
