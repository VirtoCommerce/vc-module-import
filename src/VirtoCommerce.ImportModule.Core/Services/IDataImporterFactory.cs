namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IDataImporterFactory
    {
        /// <summary>
        /// Creates an importer by its registered type name.
        /// </summary>
        /// <remarks>
        /// The importer is resolved from the provider of the scope the factory itself was resolved from, so resolve the factory
        /// in a request or job scope and never hold it in a singleton: a singleton gets the root provider,
        /// and everything the importer resolves lives until the process exits.
        /// </remarks>
        IDataImporter Create(string typeName);
    }
}
