using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ImportModule.Core.Models.Search
{
    public class SearchImportRunHistoryCriteria : SearchCriteriaBase
    {
        public string UserId { get; set; }

        public string UserName { get; set; }

        public string ProfileId { get; set; }

        private IList<string> _profileIds;

        public virtual IList<string> ProfileIds
        {
            get
            {
                if (_profileIds == null && !string.IsNullOrEmpty(ProfileId))
                {
                    _profileIds = new[] { ProfileId };
                }
                return _profileIds;
            }
            set
            {
                _profileIds = value;
            }
        }

        public string JobId { get; set; }
    }
}
