using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;

namespace VirtoCommerce.ImportModule.Data.Services;

public class LinearRegressionRemainingEstimator: IImportRemainingEstimator
{
    // Keys is x axis and matches the percentage (as [0; 1] range) of processed items
    // Values is y axis and matches the time (in ticks) when the percentage of processed items was reached
    protected Dictionary<double, double> Data = new();

    public void Start(ImportContext context)
    {
        context.ProgressInfo.EstimatingRemaining = true;

        var processedPercent = 0d;
        var processedTime = DateTime.Now.Ticks;
        // (x; y) = (0; start time)
        Data.Add(processedPercent, processedTime);
    }

    public void Update(ImportContext context)
    {
        context.ProgressInfo.EstimatingRemaining = false;
        
        var processedPercent = (double)context.ProgressInfo.ProcessedCount / context.ProgressInfo.TotalCount;
        var processedTime = DateTime.Now.Ticks;
        // (x; y) = (processed percent; processed time);
        if (Data.ContainsKey(processedPercent))
        {
            Data[processedPercent] = processedTime;
        }
        else
        {
            Data.Add(processedPercent, processedTime);
        }
    }

    public void Estimate(ImportContext context)
    {
        // x-axis (in percents as [0; 1] range)
        var percents = Data.Keys.ToArray();
        // y-axis (in ticks)
        var times = Data.Values.ToArray();

        // Get best matching line function of linear regression
        var predictionFunction = Fit.LineFunc(percents, times);

        // Predicted end time
        var predictedEndTicks = (long)Math.Ceiling(predictionFunction(1d));
        var predictedEndTime = new DateTime(predictedEndTicks);

        // Predicted remaining time
        var predictedRemaining = predictedEndTime - DateTime.Now;
        
        context.ProgressInfo.EstimatedRemaining = predictedRemaining > TimeSpan.Zero ? predictedRemaining : TimeSpan.Zero;
    }

    public void Stop(ImportContext context)
    {
        context.ProgressInfo.EstimatingRemaining = false;
        context.ProgressInfo.EstimatedRemaining = null;
    }

    public object Clone()
    {
        var clone = (LinearRegressionRemainingEstimator)MemberwiseClone();
        clone.Data = new Dictionary<double, double>(Data);
        return clone;
    }
}
