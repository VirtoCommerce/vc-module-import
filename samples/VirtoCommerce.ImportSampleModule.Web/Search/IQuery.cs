using MediatR;

namespace VirtoCommerce.ImportSampleModule.Web.Search
{
    public interface IQuery<out TResult> : IRequest<TResult>
    {
    }
}
