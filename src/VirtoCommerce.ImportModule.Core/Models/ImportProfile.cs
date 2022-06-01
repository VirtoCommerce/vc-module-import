using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Core.Models
{
    public class ImportProfile : AuditableEntity, ICloneable, IHasSellerId, IHasSettings
    {
        public string Name { get; set; }
        public string DataImporterType { get; set; }

        public string SellerId { get; set; }
        public string SellerName { get; set; }

        public ICollection<ObjectSettingEntry> Settings { get; set; }
        public string TypeName => nameof(ImportProfile);

        public string ImportFileUrl { get; set; }
        public int PreviewObjectCount { get; set; } = 10;


        public virtual void Update(ImportProfile importProfile)
        {
            DataImporterType = importProfile.DataImporterType;
            Settings = importProfile.Settings;
        }

        public virtual ImportProfile CreateNew(string sellerId, string sellerName, string name, string importer, ICollection<ObjectSettingEntry> settings)
        {
            var result = ExType<ImportProfile>.New();
            result.Name = name;
            result.DataImporterType = importer;

            result.SellerId = sellerId;
            result.SellerName = sellerName;

            result.Settings = settings;

            return result;
        }

        public object Clone()
        {
            var result = MemberwiseClone() as ImportProfile;
            result.Settings = Settings?.Select(x => x.Clone()).OfType<ObjectSettingEntry>().ToList();
            return result;
        }
    }
}
