using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Core.Models
{
    public class ImportProfile : AuditableEntity, ICloneable, IHasSettings
    {
        public string Name { get; set; }
        public string DataImporterType { get; set; }

        public string UserId { get; set; }
        public string UserName { get; set; }

        public ICollection<ObjectSettingEntry> Settings { get; set; }
        public string TypeName => nameof(ImportProfile);

        public string ImportFileUrl { get; set; }
        public string ImportReportUrl { get; set; }
        public string ImportReporterType { get; set; }
        public int PreviewObjectCount { get; set; } = 10;
   
        public virtual void Update(ImportProfile importProfile)
        {
            Name = importProfile.Name;
            DataImporterType = importProfile.DataImporterType;
            Settings = importProfile.Settings;
        }

        public object Clone()
        {
            var result = MemberwiseClone() as ImportProfile;
            result.Settings = Settings?.Select(x => x.Clone()).OfType<ObjectSettingEntry>().ToList();
            return result;
        }
    }
}
