using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IDataImporter : ICloneable
    {
        string TypeName { get; }
        Dictionary<string, string> Metadata { get; }
        SettingDescriptor[] AvailSettings { get; set; }
        IAuthorizationRequirement AuthorizationRequirement { get; set; }

        IImportDataReader OpenReader(ImportContext context);
        Task<IImportDataReader> OpenReaderAsync(ImportContext context) => Task.FromResult(OpenReader(context));
        IImportDataWriter OpenWriter(ImportContext context);
        Task<IImportDataWriter> OpenWriterAsync(ImportContext context) => Task.FromResult(OpenWriter(context));
        Task<ValidationResult> ValidateAsync(ImportContext context);
        Task OnImportCompletedAsync() { return Task.CompletedTask; }
    }
}
