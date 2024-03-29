using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;

namespace VirtoCommerce.ImportModule.Data.Models
{
    public class ImportRunHistoryEntity : AuditableEntity, IDataEntity<ImportRunHistoryEntity, ImportRunHistory>
    {
        [StringLength(128)]
        [Required]
        public string UserId { get; set; }

        [StringLength(128)]
        [Required]
        public string ProfileId { get; set; }

        [StringLength(1024)]
        public string ProfileName { get; set; }

        [StringLength(128)]
        public string JobId { get; set; }

        public DateTime Executed { get; set; }
        public DateTime? Finished { get; set; }
        public int TotalCount { get; set; }
        public int ProcessedCount { get; set; }
        public int ErrorsCount { get; set; }

        public string Errors { get; set; }

        [StringLength(2048)]
        public string FileUrl { get; set; }

        [StringLength(2048)]
        public string ReportUrl { get; set; }

        public virtual ImportRunHistoryEntity FromModel(ImportRunHistory model, PrimaryKeyResolvingMap pkMap)
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

            UserId = model.UserId;
            ProfileId = model.ProfileId;
            ProfileName = model.ProfileName;
            JobId = model.JobId;
            Executed = model.Executed;
            Finished = model.Finished;
            TotalCount = model.TotalCount;
            ProcessedCount = model.ProcessedCount;
            ErrorsCount = model.ErrorsCount;
            Errors = JsonConvert.SerializeObject(model.Errors);
            FileUrl = model.FileUrl;
            ReportUrl = model.ReportUrl;

            return this;
        }

        public virtual ImportRunHistory ToModel(ImportRunHistory model)
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

            model.UserId = UserId;
            model.ProfileId = ProfileId;
            model.ProfileName = ProfileName;
            model.JobId = JobId;
            model.Executed = Executed;
            model.Finished = Finished;
            model.TotalCount = TotalCount;
            model.ProcessedCount = ProcessedCount;
            model.ErrorsCount = ErrorsCount;
            model.Errors = JsonConvert.DeserializeObject<ICollection<string>>(Errors ?? "[]");
            model.FileUrl = FileUrl;
            model.ReportUrl = ReportUrl;

            return model;
        }

        public virtual void Patch(ImportRunHistoryEntity target)
        {
            target.ModifiedBy = ModifiedBy;
            target.ModifiedDate = ModifiedDate;

            target.Finished = Finished;
            target.TotalCount = TotalCount;
            target.ProcessedCount = ProcessedCount;
            target.ErrorsCount = ErrorsCount;
            target.Errors = Errors;
            target.FileUrl = FileUrl;
            target.ReportUrl = ReportUrl;
        }
    }
}
