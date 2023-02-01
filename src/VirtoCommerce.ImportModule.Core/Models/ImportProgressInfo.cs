using System;
using System.Collections.Generic;

namespace VirtoCommerce.ImportModule.Core.Models
{
    public class ImportProgressInfo
    {
        public DateTime? Finished { get; set; }
        public string Description { get; set; }
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public ICollection<string> Errors { get; set; } = new List<string>();
        public string ReportUrl { get; set; }
    }
}
