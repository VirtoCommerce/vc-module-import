using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportSampleModule.Web.Importers;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.ImportSampleModule.Tests.Unit
{
    public class DataImportProcessManagerTests
    {
        // Normal import progress flow
        [Fact]
        public async Task Normal_import_flow_progress_info_is_correct()
        {
            // Arrange
            var profile = new ImportProfile
            {
                DataImporterType = nameof(TestImporter),
                Settings = new List<ObjectSettingEntry>()
            };

            var sut = TestHepler.GetDataImportProcessManager();
            ImportProgressInfo progress = null;

            // Act
            await sut.ImportAsync(profile, (x) => progress = x, new CancellationToken());

            // Assertion
            progress.Should().NotBeNull();
            progress.TotalCount.Should().Be((int)TestSettings.TotalCount.DefaultValue);
            progress.ProcessedCount.Should().Be((int)TestSettings.TotalCount.DefaultValue);
            progress.Finished.Should().HaveValue();
            progress.Errors.Should().BeNullOrEmpty();
        }

        // Flow with errors
        [Fact]
        public async Task Import_error_flow_errors_are_produced()
        {
            // Arrange
            var profile = new ImportProfile
            {
                DataImporterType = nameof(TestImporter),
                Settings = new List<ObjectSettingEntry>() { new ObjectSettingEntry() { Name = "Import.Test.IsErrors", Value = true } }
            };

            var sut = TestHepler.GetDataImportProcessManager();
            ImportProgressInfo progress = null;

            // Act
            await sut.ImportAsync(profile, (x) => progress = x, new CancellationToken());

            // Assertion
            progress.TotalCount.Should().Be((int)TestSettings.TotalCount.DefaultValue);
            progress.ProcessedCount.Should().Be((int)TestSettings.TotalCount.DefaultValue);
            progress.Finished.Should().HaveValue();
            progress.Errors.Should().NotBeEmpty();
        }

        // Domain events are not emitted during import
        //[Fact]
        //public async Task Import_flow_domain_events_arent_produced()
        //{
        //    // Arrange
        //    var profile = new ImportProfile
        //    {
        //        DataImporterType = nameof(TestImporter),
        //        Settings = new List<ObjectSettingEntry>()
        //    };

        //    var sut = TestHepler.GetDataImportProcessManager();
        //    var progress = new ImportProgressInfoTest();

        //    // Act
        //    await sut.ImportAsync(profile, (x) => progress.EventsSuppressed.Add(EventSuppressor.EventsSuppressed), new CancellationToken());

        //    // Assertion
        //    progress.EventsSuppressed.Where(x => x == true).Should().HaveCount((int)TestSettings.TotalCount.DefaultValue);
        //}

        // import job cancellation
        [Fact]
        public async Task Cancel_import_import_is_cancelled()
        {
            // Arrange
            var profile = new ImportProfile
            {
                DataImporterType = nameof(TestImporter),
                Settings = new List<ObjectSettingEntry>()
            };

            var sut = TestHepler.GetDataImportProcessManager();
            ImportProgressInfo progress = null;

            // Act
            try
            {
                await sut.ImportAsync(profile, (x) => progress = x, new CancellationToken(true));
            }

            // Assertion
            catch (Exception ex)
            {
                ex.Message.Should().Be("The operation was canceled.");
                progress.ProcessedCount.Should().NotBe((int)TestSettings.TotalCount.DefaultValue);
                progress.Finished.Should().NotHaveValue();
            }
        }
    }
}
