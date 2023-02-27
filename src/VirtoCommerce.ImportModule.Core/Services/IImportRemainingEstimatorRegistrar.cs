using System.Collections.Generic;
using System;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Core.Services;

public interface IImportRemainingEstimatorRegistrar
{
    IEnumerable<IImportRemainingEstimator> AllRegistered { get; }
    ImportRemainingEstimatorBuilder Register<TImportRemainingEstimator>(Func<IImportRemainingEstimator> factory = null) where TImportRemainingEstimator : IImportRemainingEstimator;
}
