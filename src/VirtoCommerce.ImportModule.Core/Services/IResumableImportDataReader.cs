using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Core.Services
{
    /// <summary>
    /// Non-generic pattern-match target for the pipeline. Concrete readers don't implement this
    /// directly — they implement <see cref="IImportDataReader{TCursor}"/> and receive non-generic
    /// behavior via default-interface-method bridges.
    /// </summary>
    public interface IResumableImportDataReader
    {
        /// <summary>
        /// True when the current cursor represents a position from which a subsequent
        /// <see cref="TryRestoreFromSerializedCursor"/> will produce correct continuation.
        /// The pipeline gates cursor serialization on this: throttled saves are deferred while
        /// the reader is in a transient state (e.g. mid-slice of a pre-fetched batch).
        /// Default: true — most readers have a stable cursor after every ReadNextPageAsync.
        /// Readers that slice a pre-fetched batch across multiple ReadNextPageAsync calls
        /// should return false mid-slice, true at batch boundaries.
        /// </summary>
        bool HasStableCursor => true;

        /// <summary>
        /// Serialized cursor for the NEXT page the reader will produce. Stable between calls to
        /// ReadNextPageAsync. Embeds pipeline's current ProcessedCount (handled by DIM bridge).
        /// Returns null if the reader has not yet returned any page.
        /// </summary>
        string GetSerializedCursor(ImportContext context);

        /// <summary>
        /// Parses, validates, and restores reader state. Returns true on success.
        /// On true, also injects the cursor's embedded ProcessedCount into context.ProgressInfo.
        /// On false (unparseable / invalid / expired), leaves reader and context unchanged.
        /// </summary>
        bool TryRestoreFromSerializedCursor(ImportContext context, string serializedCursor);
    }
}
