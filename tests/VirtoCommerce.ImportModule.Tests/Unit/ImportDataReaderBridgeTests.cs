using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportDataReaderBridgeTests
    {
        public sealed record FakeCursor(int Skip) : ImportDataCursor;

        private sealed class FakeReader : IImportDataReader<FakeCursor>
        {
            public FakeCursor Position { get; set; } = new FakeCursor(0);
            public FakeCursor Restored { get; private set; }
            public bool ReturnNullCursor { get; set; }

            public FakeCursor GetCursor(ImportContext context) => ReturnNullCursor ? null : Position;
            public void RestoreCursor(ImportContext context, FakeCursor cursor) => Restored = cursor;

            public bool HasMoreResults => true;
            public Task<int> GetTotalCountAsync(ImportContext context) => Task.FromResult(0);
            public Task<object[]> ReadNextPageAsync(ImportContext context) => Task.FromResult<object[]>(new object[0]);
            public void Dispose() { }
        }

        [Fact]
        public void GetSerializedCursor_injects_pipeline_ProcessedCount()
        {
            var reader = new FakeReader { Position = new FakeCursor(100) };
            var profile = new ImportProfile { Settings = new List<ObjectSettingEntry>() };
            var context = new ImportContext(profile) { ProgressInfo = new ImportProgressInfo { ProcessedCount = 250 } };
            IResumableImportDataReader bridge = reader;

            var serialized = bridge.GetSerializedCursor(context);

            Assert.False(string.IsNullOrEmpty(serialized));
            var round = ImportDataCursor.Deserialize<FakeCursor>(serialized);
            Assert.Equal(100, round.Skip);
            Assert.Equal(250, round.ProcessedCount);
        }

        [Fact]
        public void TryRestoreFromSerializedCursor_extracts_PC_and_calls_RestoreCursor()
        {
            var reader = new FakeReader();
            var profile = MakeProfile(lifetimeDays: 7);
            var context = new ImportContext(profile) { ProgressInfo = new ImportProgressInfo { ProcessedCount = 0 } };

            var cursor = new FakeCursor(500) { ProcessedCount = 1000 };
            var serialized = cursor.Serialize();

            IResumableImportDataReader bridge = reader;
            var ok = bridge.TryRestoreFromSerializedCursor(context, serialized);

            Assert.True(ok);
            Assert.Equal(500, reader.Restored.Skip);
            Assert.Equal(1000, context.ProgressInfo.ProcessedCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-base64")]
        public void TryRestoreFromSerializedCursor_null_or_invalid_returns_false_no_state_change(string serializedCursor)
        {
            var reader = new FakeReader();
            var profile = MakeProfile(lifetimeDays: 7);
            var context = new ImportContext(profile) { ProgressInfo = new ImportProgressInfo { ProcessedCount = 42 } };

            IResumableImportDataReader bridge = reader;
            var ok = bridge.TryRestoreFromSerializedCursor(context, serializedCursor);

            Assert.False(ok);
            Assert.Null(reader.Restored);
            Assert.Equal(42, context.ProgressInfo.ProcessedCount);
        }

        [Fact]
        public void TryRestoreFromSerializedCursor_expired_cursor_returns_false_no_context_mutation()
        {
            var reader = new FakeReader();
            var profile = MakeProfile(lifetimeDays: 7);
            var context = new ImportContext(profile) { ProgressInfo = new ImportProgressInfo { ProcessedCount = 77 } };

            var cursor = new FakeCursor(10) { CreatedAt = DateTime.UtcNow.AddDays(-8) };
            var serialized = cursor.Serialize();

            IResumableImportDataReader bridge = reader;
            var ok = bridge.TryRestoreFromSerializedCursor(context, serialized);

            Assert.False(ok);
            Assert.Null(reader.Restored);
            Assert.Equal(77, context.ProgressInfo.ProcessedCount);
        }

        [Fact]
        public void GetSerializedCursor_returns_null_when_reader_has_no_cursor()
        {
            var reader = new FakeReader { ReturnNullCursor = true };
            var profile = new ImportProfile { Settings = new List<ObjectSettingEntry>() };
            var context = new ImportContext(profile) { ProgressInfo = new ImportProgressInfo { ProcessedCount = 250 } };
            IResumableImportDataReader bridge = reader;

            var serialized = bridge.GetSerializedCursor(context);

            Assert.Null(serialized);
        }

        private static ImportProfile MakeProfile(int lifetimeDays) =>
            new()
            {
                Settings = new List<ObjectSettingEntry>
                {
                    new()
                    {
                        Name = ImportCursorSettings.LifetimeDays.Name,
                        Value = lifetimeDays,
                        ValueType = SettingValueType.PositiveInteger,
                    },
                },
            };
    }
}
