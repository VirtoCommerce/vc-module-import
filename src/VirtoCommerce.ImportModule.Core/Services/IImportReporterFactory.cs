namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IImportReporterFactory
    {
        IImportReporter Create(string typeName);
    }
}
