using System;
using System.Collections.Generic;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Core.Services
{
    public interface IDataImporterRegistrar
    {
        IEnumerable<IDataImporter> AllRegisteredImporters { get; }

        /// <summary>
        /// Registers an importer that is resolved from the caller's DI scope on each <see cref="IDataImporterFactory.Create"/>,
        /// so the readers and writers it resolves are released when that scope ends.
        /// The importer is created with <c>ActivatorUtilities.GetServiceOrCreateInstance</c>: a service collection registration of
        /// <typeparamref name="TDataImporter"/> is used when there is one; register it as transient, because a singleton or scoped
        /// registration makes every Create return one shared instance, with its per-run state.
        /// At registration the importer is also activated once from the root provider to hold its metadata, so take scoped
        /// services from the provider while the import runs, not through the constructor.
        /// </summary>
        DataImporterBuilder Register<TDataImporter>() where TDataImporter : IDataImporter;

        /// <summary>
        /// Registers an importer through a factory that is called once; every created importer is a clone of that instance.
        /// A factory that resolves services from the root provider keeps every reader and writer the importer resolves
        /// alive until the process stops.
        /// </summary>
        [Obsolete("Use Register<TDataImporter>(). For custom construction register TDataImporter in the service collection with a factory (services.AddTransient<TDataImporter>(sp => ...)) and call Register<TDataImporter>().")]
        DataImporterBuilder Register<TDataImporter>(Func<IDataImporter> factory) where TDataImporter : IDataImporter;
    }
}
