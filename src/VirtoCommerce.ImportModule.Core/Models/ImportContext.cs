using System;
using Newtonsoft.Json;

namespace VirtoCommerce.ImportModule.Core.Models
{
    public class ImportContext
    {
        public ImportContext(ImportProfile importProfile)
        {
            ImportProfile = importProfile;
        }

        public ImportProfile ImportProfile { get; private set; }

        public ImportProgressInfo ProgressInfo { get; set; }

        public Action<ErrorInfo> ErrorCallback { get; set; }

        /// <summary>
        /// True when a pipeline has successfully restored the cursor state from a prior interrupted run.
        /// False on fresh runs or when a cursor was present but invalid/expired.
        /// </summary>
        public bool IsResume { get; set; }

        /// <summary>
        /// True when the read/write loop ended without an exception with the reader exhausted
        /// (<see cref="Services.IImportDataReader.HasMoreResults"/> false) and the final flush succeeded.
        /// Set by the pipeline before <see cref="Services.IDataImporter.OnImportCompletedAsync"/> runs.
        /// The error limit does not clear it once the source was read to the end.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// The cursor lifetime the pipeline resolved for this run: the profile's <c>Import.Cursor.LifetimeDays</c> when it stores
        /// one, otherwise the module-level setting. Set before the cursor is restored; null on a context built outside the pipeline.
        /// </summary>
        public TimeSpan? CursorLifetime { get; set; }

        /// <summary>
        /// JSON settings used by resumable readers to (de)serialize cursors. Null = Newtonsoft defaults.
        /// A reader that needs custom converters or discriminator handling populates this in its
        /// OnImportStartedAsync hook; the resume-cursor DIMs read it on save/restore.
        /// </summary>
        public JsonSerializerSettings JsonSerializerSettings { get; set; }
    }
}
