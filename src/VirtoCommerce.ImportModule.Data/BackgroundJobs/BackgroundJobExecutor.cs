using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hangfire;

namespace VirtoCommerce.ImportModule.Data.BackgroundJobs
{
    public class BackgroundJobExecutor : IBackgroundJobExecutor
    {
        public string Enqueue(Expression<Func<Task>> expression)
        {
            return BackgroundJob.Enqueue(expression);
        }

        public string Enqueue<T>(Expression<Func<T, Task>> expression)
        {
            return BackgroundJob.Enqueue(expression);
        }
    }
}
