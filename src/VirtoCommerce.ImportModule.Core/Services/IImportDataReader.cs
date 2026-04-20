using System;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IImportDataReader : IDisposable
    {
        Task<int> GetTotalCountAsync(ImportContext context);

        Task<object[]> ReadNextPageAsync(ImportContext context);

        bool HasMoreResults { get; }
    }

    public interface IImportDataReader<TCursor> : IImportDataReader, IResumableImportDataReader
        where TCursor : ImportDataCursor
    {
        /// <summary>Cursor identifying the NEXT page the reader will produce.</summary>
        TCursor GetCursor(ImportContext context);

        /// <summary>Restores reader state so the next ReadNextPageAsync produces the cursor's page.</summary>
        void RestoreCursor(ImportContext context, TCursor cursor);

        // DIM: injects pipeline's current ProcessedCount into the cursor before serializing.
        string IResumableImportDataReader.GetSerializedCursor(ImportContext context)
        {
            var cursor = GetCursor(context);
            if (cursor is null)
            {
                return null;
            }
            var snapshot = cursor with { ProcessedCount = context.ProgressInfo?.ProcessedCount ?? 0 };

            return snapshot.Serialize(context.JsonSerializerSettings);
        }

        // DIM: restores reader state, then injects cursor's embedded PC back into pipeline.
        bool IResumableImportDataReader.TryRestoreFromSerializedCursor(ImportContext context, string serializedCursor)
        {
            var cursor = ImportDataCursor.Deserialize<TCursor>(serializedCursor, context.JsonSerializerSettings);
            if (cursor is null || !cursor.IsValid(context))
            {
                return false;
            }

            RestoreCursor(context, cursor);
            context.ProgressInfo?.ProcessedCount = cursor.ProcessedCount;

            return true;
        }
    }
}
