using System.Collections.Generic;
using CsvHelper.Configuration;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.CsvHelper
{
    public abstract class ClassMapExtended<TClass> : ClassMap<TClass>
    {
        public ClassMapExtended() : base() { }
        public static ICollection<ObjectSettingEntry> Settings { get; set; }

    }
}
