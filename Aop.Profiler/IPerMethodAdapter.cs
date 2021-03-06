﻿using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Aop.Profiler
{
    public interface IPerMethodAdapter<T> : IProfilerAdapter<T> where T : class
    {
        IPerMethodAdapter<T> Profile<TReturn>(Expression<Func<T, Task<TReturn>>> target, CaptureOptions captureOptions = CaptureOptions.Default);
        IPerMethodAdapter<T> Profile(Expression<Func<T, Task>> target, CaptureOptions captureOptions = CaptureOptions.Default);
        IPerMethodAdapter<T> Profile(Expression<Action<T>> target, CaptureOptions captureOptions = CaptureOptions.Default);
        IPerMethodAdapter<T> Profile<TReturn>(Expression<Func<T, TReturn>> target, CaptureOptions captureOptions = CaptureOptions.Default);
    }
}