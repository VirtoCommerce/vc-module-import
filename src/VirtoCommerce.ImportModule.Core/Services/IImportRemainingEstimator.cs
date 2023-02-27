using System;
using VirtoCommerce.ImportModule.Core.Models;

namespace VirtoCommerce.ImportModule.Core.Services;

public interface IImportRemainingEstimator: ICloneable
{
    void Start(ImportContext context);

    void Update(ImportContext context);

    void Estimate(ImportContext context);
    
    void Stop(ImportContext context);
}
