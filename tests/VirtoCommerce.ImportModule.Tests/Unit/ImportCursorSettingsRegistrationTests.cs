using System.Collections.Generic;
using System.Linq;
using Moq;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.ImportModule.Web;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportCursorSettingsRegistrationTests
    {
        [Fact]
        public void Module_Registers_Cursor_Settings_For_Import_Profiles()   // U12
        {
            var registrar = new Mock<ISettingsRegistrar>();

            Module.RegisterSettings(registrar.Object, "VirtoCommerce.Import");

            registrar.Verify(x => x.RegisterSettingsForType(
                It.Is<IEnumerable<SettingDescriptor>>(s => s.Select(d => d.Name).OrderBy(n => n)
                    .SequenceEqual(ImportCursorSettings.AllSettings.Select(d => d.Name).OrderBy(n => n))),
                nameof(ImportProfile)), Times.Once);
        }

        [Fact]
        public void Process_Model_Keeps_Cursor_Settings_When_Importer_Declares_Its_Own()   // U13
        {
            var ownSetting = new SettingDescriptor { Name = "Importer.Own", ValueType = SettingValueType.ShortText };
            var importer = new Mock<IDataImporter>();
            importer.SetupGet(x => x.TypeName).Returns("TestImporter");
            importer.SetupGet(x => x.AvailSettings).Returns([ownSetting]);
            var registrar = new Mock<IDataImporterRegistrar>();
            registrar.SetupGet(x => x.AllRegisteredImporters).Returns([importer.Object]);
            var service = new TestableCrudService(registrar.Object, new Mock<ISettingsManager>().Object);
            var model = new ImportProfile
            {
                DataImporterType = "TestImporter",
                Settings =
                [
                    new ObjectSettingEntry { Name = ownSetting.Name, Value = "x" },
                    new ObjectSettingEntry { Name = ImportCursorSettings.LifetimeDays.Name, Value = 3 },
                    new ObjectSettingEntry { Name = ImportCursorSettings.SaveIntervalPages.Name, Value = 5 },
                    new ObjectSettingEntry { Name = "Foreign", Value = "y" },
                ],
            };

            var result = service.ProcessModelForTest(model);

            Assert.Equal(
                [ownSetting.Name, ImportCursorSettings.LifetimeDays.Name, ImportCursorSettings.SaveIntervalPages.Name],
                result.Settings.Select(x => x.Name));
        }

        private sealed class TestableCrudService(IDataImporterRegistrar registrar, ISettingsManager settingsManager)
            : ImportProfileCrudService(registrar, () => null, Mock.Of<IPlatformMemoryCache>(), Mock.Of<IEventPublisher>(), settingsManager)
        {
            public ImportProfile ProcessModelForTest(ImportProfile model) => ProcessModel(null, null, model);
        }
    }
}
