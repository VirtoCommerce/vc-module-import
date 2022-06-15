# Additional development information.

To create a Custom Importer, the developer needs to define their own DataReader and DataWriter, and define Settings and Validation rules, if necessary. Currently, as a help, the Module provides the ability to use the CSV Reader as part of the [VirtoCommerce.ImportModule.CsvHelper](https://www.nuget.org/packages/VirtoCommerce.ImportModule.CsvHelper) package, but does not restrict the developer from writing their own Readers for any data sources.

> ***Note:*** *As an example, the process of implementing a Custom Importer using CsvHelper is detailed [here](03-building-custom-importer.md).*


The following Module services are also available for developers, which can be used in the UI:

-   `IImportRunService` is the main service for managing the execution of the import process. The service provides access to methods for previewing (_PreviewAsync_), launching an import (_RunImportAsync_), and creating a similar import task in the form of a BackgroundJob (_RunImportBackgroundJob_).

-   Services `IImportProfilesSearchService` and `IImportRunHistorySearchService` - provide search capabilities for _ImportProfiles_ and _ImportRunHistory_, respectively.

-   Services `IImportProfileCrudService` and `IImportRunHistoryCrudService` - provide facilities for CRUD operations on _ImportProfiles_ and _ImportRunHistory_.


For developers, the possibility of extending authorization for newly created Importers is available. The `DataImporterBuilder` has _WithAuthorizationReqirement_ and _WithAuthorizationPermission_ methods to set custom permissions if necessary.

