using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ImportModule.Data.Services;

public class ImportRemainingEstimatorRegistar : IImportRemainingEstimatorRegistar, IImportRemainingEstimatorFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public IEnumerable<IImportRemainingEstimator> AllRegistered
    {
        get
        {
            return AbstractTypeFactory<IImportRemainingEstimator>.AllTypeInfos.Select(x => AbstractTypeFactory<IImportRemainingEstimator>.TryCreateInstance(x.TypeName));
        }
    }

    public ImportRemainingEstimatorRegistar(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ImportRemainingEstimatorBuilder Register<TImportRemainingEstimator>(Func<IImportRemainingEstimator> factory = null)
        where TImportRemainingEstimator : IImportRemainingEstimator
    {
        var typeInfo = AbstractTypeFactory<IImportRemainingEstimator>.RegisterType<TImportRemainingEstimator>();
        var builder = new ImportRemainingEstimatorBuilder(_serviceProvider, typeof(TImportRemainingEstimator), factory);
        typeInfo.WithFactory(() => builder.Build());
        return builder;
    }

    public IImportRemainingEstimator Create(string typeName)
    {
        return AbstractTypeFactory<IImportRemainingEstimator>.TryCreateInstance(typeName);
    }
}
