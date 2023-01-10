using System;
using System.ComponentModel.DataAnnotations;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;

namespace VirtoCommerce.ImportModule.Data.Models
{
    public class ImportProfileEntity : AuditableEntity, IDataEntity<ImportProfileEntity, ImportProfile>
    {
        [StringLength(1024)]
        [Required]
        public string Name { get; set; }

        [StringLength(254)]
        [Required]
        public string DataImporterType { get; set; }

        [StringLength(128)]
        [Required]
        public string UserId { get; set; }

        [StringLength(254)]
        [Required]
        public string UserName { get; set; }

        public virtual ImportProfileEntity FromModel(ImportProfile model, PrimaryKeyResolvingMap pkMap)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            pkMap.AddPair(model, this);

            Id = model.Id;
            CreatedBy = model.CreatedBy;
            CreatedDate = model.CreatedDate;
            ModifiedBy = model.ModifiedBy;
            ModifiedDate = model.ModifiedDate;

            Name = model.Name;
            DataImporterType = model.DataImporterType;
            UserId = model.UserId;
            UserName = model.UserName;

            return this;
        }

        public virtual ImportProfile ToModel(ImportProfile model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            model.Id = Id;
            model.CreatedBy = CreatedBy;
            model.CreatedDate = CreatedDate;
            model.ModifiedBy = ModifiedBy;
            model.ModifiedDate = ModifiedDate;

            model.Name = Name;
            model.DataImporterType = DataImporterType;
            model.UserId = UserId;
            model.UserName = UserName;

            return model;
        }

        public virtual void Patch(ImportProfileEntity target)
        {
            target.DataImporterType = DataImporterType;
            target.Name = Name;
        }
    }
}
