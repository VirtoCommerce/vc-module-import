using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportSampleModule.Web.Models
{
    public class MyCustomImportProfile : ImportProfile
    {
        public MyCustomImportProfile()
            : base()
        {
            ProfileType = nameof(MyCustomImportProfile);
        }
        public string MyCustomField { get; set; }
    }
}
