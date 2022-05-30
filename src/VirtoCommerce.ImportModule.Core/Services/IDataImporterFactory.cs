namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IDataImporterFactory
    {
        IDataImporter Create(string typeName);
    }
}
