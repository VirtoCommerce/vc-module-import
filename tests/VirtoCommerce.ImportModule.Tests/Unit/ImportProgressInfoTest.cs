using System.Collections.Generic;
using VirtoCommerce.ImportModule.Data.Models;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    class ImportProgressInfoTest : ImportProgressInfo
    {
        public ICollection<bool> EventsSuppressed { get; set; } = new List<bool>();
    }
}
