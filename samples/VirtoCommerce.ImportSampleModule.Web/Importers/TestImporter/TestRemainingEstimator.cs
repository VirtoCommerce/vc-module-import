using System;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;

namespace VirtoCommerce.ImportSampleModule.Web.Importers;

public class TestRemainingEstimator: IImportRemainingEstimator
{
    public void Start(ImportContext context)
    {
        context.ProgressInfo.EstimatingRemaining = true;
    }

    public void Update(ImportContext context)
    {
        context.ProgressInfo.EstimatingRemaining = false;
    }

    public void Estimate(ImportContext context)
    {
        context.ProgressInfo.EstimatedRemaining = TimeSpan.FromSeconds(10);
    }

    public void Stop(ImportContext context)
    {
        context.ProgressInfo.EstimatedRemaining = null;
    }
    
    public object Clone()
    {
        return MemberwiseClone();
    }
}
