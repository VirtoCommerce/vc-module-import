using System;
using VirtoCommerce.ImportModule.Core.Services;

namespace VirtoCommerce.ImportModule.Core.Models;

public class ImportRemainingEstimatorBuilder
{
    public ImportRemainingEstimatorBuilder(IServiceProvider serviceProvider, Type remainingEstimatorType, Func<IImportRemainingEstimator> factory = null)
    {
        ServiceProvider = serviceProvider;
        ImportRemainingEstimator = factory != null ? factory() : Activator.CreateInstance(remainingEstimatorType) as IImportRemainingEstimator;
    }

    public IImportRemainingEstimator ImportRemainingEstimator { get; init; }

    public IServiceProvider ServiceProvider { get; init; }
    
    public IImportRemainingEstimator Build()
    {
        return ImportRemainingEstimator.Clone() as IImportRemainingEstimator;
    }
}
