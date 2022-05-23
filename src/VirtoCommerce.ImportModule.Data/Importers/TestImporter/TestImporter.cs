using System.Collections.Generic;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Data.Importers
{
    public class TestImporter : IDataImporter
    {
        public virtual string TypeName { get; } = nameof(TestImporter);
        public virtual Dictionary<string, string> Metadata { get; }
        public virtual SettingDescriptor[] AvailSettings { get; set; }

        public IImportDataReader OpenReader(ImportContext context)
        {
            return new TestDataReader(context);
        }

        public IImportDataWriter OpenWriter(ImportContext context)
        {
            return new TestDataWriter();
        }

        public object Clone()
        {
            return base.MemberwiseClone();
        }
    }
}
