using System;
using System.Collections.Generic;
using VirtoCommerce.ImportModule.Data.Models;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Data.Services
{
    public interface IDataImporter : ICloneable
    {
        string TypeName { get;  }
        Dictionary<string, string> Metadata { get; }
        SettingDescriptor[] AvailSettings { get; set; }

        IImportDataReader OpenReader(ImportContext context);
        IImportDataWriter OpenWriter(ImportContext context);

    }
}
