using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class TestImporter : IDataImporter
    {
        public virtual string TypeName => nameof(TestImporter);
        public virtual Dictionary<string, string> Metadata { get; }
        public virtual SettingDescriptor[] AvailSettings { get; set; }
        public IAuthorizationRequirement AuthorizationRequirement { get; set; }

        public IImportDataReader OpenReader(ImportContext context)
        {
            return new TestDataReader(context);
        }

        public IImportDataWriter OpenWriter(ImportContext context)
        {
            return new TestDataWriter();
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public Task<ValidationResult> ValidateAsync(ImportContext context)
        {
            return Task.FromResult(new ValidationResult());
        }
    }
}
