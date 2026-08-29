namespace VirtoCommerce.ImportModule.CsvHelper.Services
{
    public interface IClassMapFactory<TClassMap>
    {
        TClassMap Create(string typeName);
    }
}
