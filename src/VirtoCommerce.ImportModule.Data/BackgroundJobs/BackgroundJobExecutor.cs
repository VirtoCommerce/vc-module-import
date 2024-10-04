using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hangfire;

namespace VirtoCommerce.ImportModule.Data.BackgroundJobs
{
    public class BackgroundJobExecutor : IBackgroundJobExecutor
    {
        public virtual string Enqueue(Expression<Func<Task>> expression)
        {
            return BackgroundJob.Enqueue(expression);
        }

        public virtual string Enqueue(string queue, Expression<Func<Task>> expression)
        {
            return BackgroundJob.Enqueue(queue, expression);
        }

        public virtual string Enqueue<T>(Expression<Func<T, Task>> expression)
        {
            return BackgroundJob.Enqueue(expression);
        }

        public virtual string Enqueue<T>(string queue, Expression<Func<T, Task>> expression)
        {
            return BackgroundJob.Enqueue(queue, expression);
        }
    }
}
