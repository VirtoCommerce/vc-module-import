using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Core.Domains.ImportProfileAggregate.Events;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Core.Domains
{
    public class ImportProfile : AggregateRoot, IHasSellerId, IHasSettings
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

        public virtual void Run(ImportPushNotification notification)
        {
            AddDomainEvent(new ImportStartedDomainEvent { ImportProfile = this, Notification = notification });
        }

        public virtual void Finish(ImportPushNotification notification)
        {
            AddDomainEvent(new ImportFinishedDomainEvent { ImportProfile = this, Notification = notification });
        }

        public virtual void Abort(Exception exception, ImportPushNotification notification)
        {
            AddDomainEvent(new ImportFinishedDomainEvent { Exception = exception, ImportProfile = this, Notification = notification });
        }

        public override object Clone()
        {
            var result = base.Clone() as ImportProfile;
            result.Settings = Settings?.Select(x => x.Clone()).OfType<ObjectSettingEntry>().ToList();
            return result;
        }
    }
}
