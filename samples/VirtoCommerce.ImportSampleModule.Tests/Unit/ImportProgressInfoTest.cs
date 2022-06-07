using System.Collections.Generic;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportSampleModule.Tests.Unit
{
    class ImportProgressInfoTest : ImportProgressInfo
    {
        public ICollection<bool> EventsSuppressed { get; set; } = new List<bool>();
    }
}
