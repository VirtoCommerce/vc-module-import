using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class DataImportProcessManagerCursorSettingsTests
    {
        private sealed record PageCursor(int PageIdx) : ImportDataCursor;

        private sealed class PagedReader : IImportDataReader<PageCursor>
        {
            public int PageIdx { get; private set; }
            public int TotalPages { get; init; } = 5;
            public bool HasMoreResults => PageIdx < TotalPages;
            public PageCursor GetCursor(ImportContext context) => new(PageIdx);
            public void RestoreCursor(ImportContext context, PageCursor cursor) => PageIdx = cursor.PageIdx;
            public Task<int> GetTotalCountAsync(ImportContext context) => Task.FromResult(TotalPages * 10);
            public Task<object[]> ReadNextPageAsync(ImportContext context)
            {
                PageIdx++;
                return Task.FromResult(new object[10]);
            }
            public void Dispose() { }
        }

        private sealed class TraceWriter : IImportDataWriter
        {
            public int Flushes { get; private set; }
            public Task WriteAsync(object[] items, ImportContext context) => Task.CompletedTask;
            public Task FlushAsync(ImportContext context)
            {
                Flushes++;
                return Task.CompletedTask;
            }
            public void Dispose() { }
        }

        private static Action<Mock<ISettingsManager>> ModuleSettings(int? saveIntervalPages = null, int? lifetimeDays = null)
        {
            return settings =>
            {
                if (saveIntervalPages is not null)
                {
                    settings
                        .Setup(x => x.GetObjectSettingAsync(ImportCursorSettings.SaveIntervalPages.Name, It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(new ObjectSettingEntry { Value = saveIntervalPages.Value });
                }

                if (lifetimeDays is not null)
                {
                    settings
                        .Setup(x => x.GetObjectSettingAsync(ImportCursorSettings.LifetimeDays.Name, It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(new ObjectSettingEntry { Value = lifetimeDays.Value });
                }
            };
        }

        private static ImportProfile MakeProfile(int? saveIntervalPages = null, int? lifetimeDays = null, ImportRunHistory history = null)
        {
            var settings = new List<ObjectSettingEntry>();
            if (saveIntervalPages is not null)
            {
                settings.Add(new ObjectSettingEntry { Name = ImportCursorSettings.SaveIntervalPages.Name, Value = saveIntervalPages.Value, ValueType = SettingValueType.PositiveInteger });
            }

            if (lifetimeDays is not null)
            {
                settings.Add(new ObjectSettingEntry { Name = ImportCursorSettings.LifetimeDays.Name, Value = lifetimeDays.Value, ValueType = SettingValueType.PositiveInteger });
            }

            return new ImportProfile { Settings = settings.Count > 0 ? settings : null, RunHistory = history };
        }

        private static async Task<(int Flushes, int Checkpoints)> RunAndCountCheckpoints(ImportProfile profile, Action<Mock<ISettingsManager>> configureSettings)
        {
            var writer = new TraceWriter();
            var checkpoints = 0;
            var manager = TestHelperFactory.CreateManagerWithImporter(new PagedReader(), writer, configureSettings: configureSettings);

            await manager.ImportAsync(profile, info =>
            {
                if (info.ShouldSaveHistory)
                {
                    checkpoints++;
                }

                return Task.CompletedTask;
            }, CancellationToken.None);

            return (writer.Flushes, checkpoints);
        }

        private static async Task<bool> RunAndCaptureIsResume(ImportProfile profile, Action<Mock<ISettingsManager>> configureSettings)
        {
            var isResume = false;
            var manager = TestHelperFactory.CreateManagerWithImporter(new PagedReader(), new TraceWriter(),
                configureImporter: importer => importer
                    .Setup(x => x.OnImportStartedAsync(It.IsAny<ImportContext>()))
                    .Callback<ImportContext>(x => isResume = x.IsResume)
                    .Returns(Task.CompletedTask),
                configureSettings: configureSettings);

            await manager.ImportAsync(profile, _ => Task.CompletedTask, CancellationToken.None);

            return isResume;
        }

        private static ImportRunHistory HistoryWithCursorCreatedDaysAgo(int days)
        {
            var cursor = new PageCursor(2) { ProcessedCount = 20, CreatedAt = DateTime.UtcNow.AddDays(-days) };

            return new ImportRunHistory { Id = "H1", Cursor = cursor.Serialize(), ProcessedCount = 20 };
        }

        [Fact]
        public async Task Module_Setting_Sets_The_Checkpoint_Interval_When_The_Profile_Stores_None()
        {
            var (flushes, checkpoints) = await RunAndCountCheckpoints(MakeProfile(), ModuleSettings(saveIntervalPages: 2));

            // Interval 2 over 5 pages: checkpoints at the top of iterations 3 and 5, plus the flush in finally
            Assert.Equal(2, checkpoints);
            Assert.Equal(3, flushes);
        }

        [Fact]
        public async Task Unset_Profile_Entry_Falls_Through_To_The_Module_Checkpoint_Interval()
        {
            var profile = new ImportProfile { Settings = [new ObjectSettingEntry(ImportCursorSettings.SaveIntervalPages)] };

            var (flushes, checkpoints) = await RunAndCountCheckpoints(profile, ModuleSettings(saveIntervalPages: 2));

            // The platform materialises a registered-but-unset setting with a null Value; it must not shadow the module value
            Assert.Equal(2, checkpoints);
            Assert.Equal(3, flushes);
        }

        [Fact]
        public async Task Profile_Value_Wins_Over_The_Module_Checkpoint_Interval()
        {
            var (flushes, checkpoints) = await RunAndCountCheckpoints(MakeProfile(saveIntervalPages: 1000), ModuleSettings(saveIntervalPages: 2));

            Assert.Equal(0, checkpoints);
            Assert.Equal(1, flushes);
        }

        [Fact]
        public async Task Module_Lifetime_Expires_A_Cursor_When_The_Profile_Stores_None()
        {
            var expired = await RunAndCaptureIsResume(MakeProfile(history: HistoryWithCursorCreatedDaysAgo(2)), ModuleSettings(lifetimeDays: 1));
            var valid = await RunAndCaptureIsResume(MakeProfile(history: HistoryWithCursorCreatedDaysAgo(2)), ModuleSettings(lifetimeDays: 7));

            Assert.False(expired);
            Assert.True(valid);
        }

        [Fact]
        public async Task Profile_Lifetime_Wins_Over_The_Module_Lifetime()
        {
            var isResume = await RunAndCaptureIsResume(MakeProfile(lifetimeDays: 1, history: HistoryWithCursorCreatedDaysAgo(2)), ModuleSettings(lifetimeDays: 7));

            Assert.False(isResume);
        }
    }
}
