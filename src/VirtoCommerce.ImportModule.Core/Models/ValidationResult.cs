using System.Collections.Generic;

namespace VirtoCommerce.ImportModule.Core.Models
{
    public class ValidationResult
    {
        public ICollection<string> Errors { get; set; } = new List<string>();
        public int ErrorsCount => Errors?.Count ?? 0;
    }
}
