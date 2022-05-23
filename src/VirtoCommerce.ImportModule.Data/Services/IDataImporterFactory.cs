namespace VirtoCommerce.ImportModule.Data.Services
{
    public interface IDataImporterFactory
    {
        IDataImporter Create(string typeName);
    }
}
