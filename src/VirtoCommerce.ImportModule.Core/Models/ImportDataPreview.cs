using System.Collections.Generic;

namespace VirtoCommerce.ImportModule.Core.Models
{
    public class ImportDataPreview
    {
        public int TotalCount { get; set; }
        public string FileName { get; set; }
        public object[] Records { get; set; }
        public ICollection<string> Errors { get; set; } = new List<string>();
    }
}
