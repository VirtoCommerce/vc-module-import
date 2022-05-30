using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ImportModule.Core.Common
{
    public static class ExType<T>
    {
        public static T New()
        {
            return AbstractTypeFactory<T>.TryCreateInstance();
        }
    }
}
