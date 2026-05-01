using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportDataReaderBridgeTests
    {
        private sealed record FakeCursor(int Skip) : ImportDataCursor;

        private sealed class FakeReader : IImportDataReader<FakeCursor>
        {
            public FakeCursor Position { get; set; } = new(0);
            public FakeCursor Restored { get; private set; }
            public bool ReturnNullCursor { get; set; }

            public FakeCursor GetCursor(ImportContext context) => ReturnNullCursor ? null : Position;
            public void RestoreCursor(ImportContext context, FakeCursor cursor) => Restored = cursor;

            public bool HasMoreResults => true;
            public Task<int> GetTotalCountAsync(ImportContext context) => Task.FromResult(0);
            public Task<object[]> ReadNextPageAsync(ImportContext context) => Task.FromResult<object[]>([]);
            public void Dispose() { }
        }

        [Fact]
        public void GetSerializedCursor_Injects_Pipeline_ProcessedCount()
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
        public void TryRestoreFromSerializedCursor_Extracts_PC_And_Calls_RestoreCursor()
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
        public void TryRestoreFromSerializedCursor_Null_Or_Invalid_Returns_False_No_State_Change(string serializedCursor)
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
        public void TryRestoreFromSerializedCursor_Expired_Cursor_Returns_False_No_Context_Mutation()
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
        public void GetSerializedCursor_Uses_JsonSerializerSettings_From_Context()
        {
            var reader = new FakeReader { Position = new FakeCursor(42) };
            var profile = new ImportProfile { Settings = new List<ObjectSettingEntry>() };
            var context = new ImportContext(profile)
            {
                ProgressInfo = new ImportProgressInfo { ProcessedCount = 1 },
                JsonSerializerSettings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                },
            };
            IResumableImportDataReader bridge = reader;

            var serialized = bridge.GetSerializedCursor(context);

            // TypeNameHandling.All embeds "$type" in the JSON — observable proof settings were honored.
            var decodedJson = Encoding.UTF8.GetString(Convert.FromBase64String(serialized));
            Assert.Contains("\"$type\":", decodedJson);
        }

        [Fact]
        public void TryRestoreFromSerializedCursor_Uses_JsonSerializerSettings_From_Context()
        {
            var reader = new FakeReader();
            var profile = MakeProfile(lifetimeDays: 7);

            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            var cursor = new FakeCursor(7) { ProcessedCount = 11 };
            var serialized = cursor.Serialize(settings);

            var context = new ImportContext(profile)
            {
                ProgressInfo = new ImportProgressInfo { ProcessedCount = 0 },
                JsonSerializerSettings = settings,
            };
            IResumableImportDataReader bridge = reader;

            var ok = bridge.TryRestoreFromSerializedCursor(context, serialized);

            Assert.True(ok);
            Assert.Equal(7, reader.Restored.Skip);
            Assert.Equal(11, context.ProgressInfo.ProcessedCount);
        }

        [Fact]
        public void GetSerializedCursor_Returns_Null_When_Reader_Has_No_Cursor()
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
