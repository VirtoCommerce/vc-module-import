using System;
using System.Collections.Generic;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportDataCursorTests
    {
        public sealed record TestCursor(int Skip, string PageToken) : ImportDataCursor;

        [Fact]
        public void Serialize_Then_Deserialize_Round_Trip_Preserves_Fields()
        {
            var cursor = new TestCursor(100, "abc")
            {
                ProcessedCount = 250,
                CreatedAt = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc),
            };
            var serialized = cursor.Serialize();

            var restored = ImportDataCursor.Deserialize<TestCursor>(serialized);

            Assert.NotNull(restored);
            Assert.Equal(100, restored.Skip);
            Assert.Equal("abc", restored.PageToken);
            Assert.Equal(250, restored.ProcessedCount);
            Assert.Equal(cursor.CreatedAt, restored.CreatedAt);
        }

        [Fact]
        public void Deserialize_Null_Or_Empty_Returns_Null()
        {
            Assert.Null(ImportDataCursor.Deserialize<TestCursor>(null));
            Assert.Null(ImportDataCursor.Deserialize<TestCursor>(string.Empty));
        }

        [Fact]
        public void Deserialize_Garbage_String_Returns_Null_Not_Throws()
        {
            Assert.Null(ImportDataCursor.Deserialize<TestCursor>("not-base64!!!"));
            Assert.Null(ImportDataCursor.Deserialize<TestCursor>("YWJjZA==")); // valid base64 but not JSON
        }

        [Fact]
        public void IsValid_Within_TTL_Returns_True()
        {
            var cursor = new TestCursor(0, null) { CreatedAt = DateTime.UtcNow.AddDays(-1) };
            Assert.True(cursor.IsValid(MakeContextWithLifetimeDays(7)));
        }

        [Fact]
        public void IsValid_Past_TTL_Returns_False()
        {
            var cursor = new TestCursor(0, null) { CreatedAt = DateTime.UtcNow.AddDays(-8) };
            Assert.False(cursor.IsValid(MakeContextWithLifetimeDays(7)));
        }

        [Fact]
        public void IsValid_Falls_Back_To_Descriptor_Default_When_Setting_Absent()
        {
            var cursor = new TestCursor(0, null) { CreatedAt = DateTime.UtcNow.AddDays(-3) };
            var profile = new ImportProfile { Settings = new List<ObjectSettingEntry>() };
            var context = new ImportContext(profile);
            // Default LifetimeDays descriptor is 7 — 3 days old cursor still valid.
            Assert.True(cursor.IsValid(context));
        }

        private static ImportContext MakeContextWithLifetimeDays(int days)
        {
            var profile = new ImportProfile
            {
                Settings = new List<ObjectSettingEntry>
                {
                    new()
                    {
                        Name = ImportCursorSettings.LifetimeDays.Name,
                        Value = days,
                        ValueType = SettingValueType.PositiveInteger,
                    },
                },
            };
            return new ImportContext(profile);
        }
    }
}
