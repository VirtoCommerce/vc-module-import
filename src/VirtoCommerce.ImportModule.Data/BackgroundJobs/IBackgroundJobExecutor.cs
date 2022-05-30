using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace VirtoCommerce.ImportModule.Data.BackgroundJobs
{
    public interface IBackgroundJobExecutor
    {
        string Enqueue(Expression<Func<Task>> expression);
        string Enqueue<T>(Expression<Func<T, Task>> expression);
    }
}
