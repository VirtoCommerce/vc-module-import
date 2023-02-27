namespace VirtoCommerce.ImportModule.Core.Services;

public interface IImportRemainingEstimatorFactory
{
    IImportRemainingEstimator Create(string typeName);
}
