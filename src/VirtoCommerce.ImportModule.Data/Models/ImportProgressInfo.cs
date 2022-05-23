using System;
using System.Collections.Generic;

namespace VirtoCommerce.ImportModule.Data.Models
{
    public class ImportProgressInfo
    {
        public DateTime? Finished { get; set; } 
        public string Description { get; set; }
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public ICollection<string> Errors { get; set; } = new List<string>();
    }
}
