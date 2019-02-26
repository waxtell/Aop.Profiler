using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Aop.Profiler
{
    public interface IPerMethodAdapter<T> where T : class
    {
        T Object { get; }
        IPerMethodAdapter<T> Profile<TReturn>(Expression<Func<T, Task<TReturn>>> target, CaptureOptions captureOptions);
        IPerMethodAdapter<T> Profile(Expression<Func<T, Task>> target, CaptureOptions captureOptions);
        IPerMethodAdapter<T> Profile(Expression<Action<T>> target, CaptureOptions captureOptions);
        IPerMethodAdapter<T> Profile<TReturn>(Expression<Func<T, TReturn>> target, CaptureOptions captureOptions);
    }
}