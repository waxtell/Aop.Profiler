using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Aop.Profiler.EventProcessing;
using Newtonsoft.Json;

namespace Aop.Profiler
{
    using EnqueueDelegate = Action<CaptureOptions, DateTime, IInvocation>;

    public abstract class BaseAdapter<T> : IInterceptor where T : class
    {
        protected readonly IProcessProfilerEvents EventProcessor;

        protected readonly List<(Expectation expectation, EnqueueDelegate)> Expectations = new List<(Expectation,EnqueueDelegate)>();

        protected void Enqueue(CaptureOptions captureOptions, DateTime startUtc, DateTime endUtc, IInvocation invocation, object returnValue)
        {
            try
            {
                var @event = new Dictionary<string, object>();

                if (captureOptions.HasFlag(CaptureOptions.ThreadId))
                {
                    @event.Add(nameof(CaptureOptions.ThreadId), Thread.CurrentThread.ManagedThreadId);
                }

                if (captureOptions.HasFlag(CaptureOptions.StartDateTimeUtc))
                {
                    @event.Add(nameof(CaptureOptions.StartDateTimeUtc), startUtc);
                }

                if (captureOptions.HasFlag(CaptureOptions.EndDateTimeUtc))
                {
                    @event.Add(nameof(CaptureOptions.EndDateTimeUtc), endUtc);
                }

                if (captureOptions.HasFlag(CaptureOptions.Duration))
                {
                    @event.Add(nameof(CaptureOptions.Duration), endUtc.Subtract(startUtc));
                }

                if (captureOptions.HasFlag(CaptureOptions.SerializedInputParameters))
                {
                    @event.Add(nameof(CaptureOptions.SerializedInputParameters), JsonConvert.SerializeObject(invocation.Arguments));
                }

                if (captureOptions.HasFlag(CaptureOptions.MethodName))
                {
                    @event.Add(nameof(CaptureOptions.MethodName), invocation.Method.Name);
                }

                if (captureOptions.HasFlag(CaptureOptions.SerializedResult))
                {
                    @event.Add(nameof(CaptureOptions.SerializedResult), JsonConvert.SerializeObject(returnValue));
                }

                if (captureOptions.HasFlag(CaptureOptions.DeclaringTypeName))
                {
                    @event.Add(nameof(CaptureOptions.DeclaringTypeName), invocation.TargetType.FullName);
                }

                EventProcessor.ProcessEvent(@event);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
        }

        protected EnqueueDelegate BuildDelegateForAsynchronousAction()
        {
            Expression<EnqueueDelegate>
                expr =
                    (captureOptions, start, invocation) => (invocation.ReturnValue as Task)
                        .ContinueWith
                        (
                            i => Enqueue(captureOptions & ~CaptureOptions.SerializedResult, start, DateTime.UtcNow, invocation, null)
                        );

            return expr.Compile();
        }

        protected EnqueueDelegate BuildDelegateForSynchronousAction()
        {
            Expression<EnqueueDelegate>
                expr =
                    (captureOptions, start, invocation) 
                        => Enqueue
                            (
                                captureOptions & ~CaptureOptions.SerializedResult, 
                                start, DateTime.UtcNow, 
                                invocation, 
                                null
                            );

            return expr.Compile();
        }

        protected EnqueueDelegate BuildDelegateForAsynchronousFunc<TReturn>()
        {
            Expression<EnqueueDelegate>
                expr =
                    (captureOptions, start, invocation) => (invocation.ReturnValue as Task<TReturn>)
                                                                .ContinueWith
                                                                (
                                                                    i => Enqueue(captureOptions, start,DateTime.UtcNow,invocation, i.Result)
                                                                );

            return expr.Compile();
        }

        protected EnqueueDelegate BuildDelegateForSynchronousFunc()
        {
            Expression<EnqueueDelegate>
                expr =
                    (captureOptions, start, invocation) => Enqueue(captureOptions, start,DateTime.UtcNow,invocation,invocation.ReturnValue);

            return expr.Compile();
        }

        public abstract void Intercept(IInvocation invocation);

        public T Adapt(T instance)
        {
            return
                typeof(T).GetTypeInfo().IsInterface
                    ? new ProxyGenerator().CreateInterfaceProxyWithTarget(instance, this)
                    : new ProxyGenerator().CreateClassProxyWithTarget(instance, this);
        }

        protected BaseAdapter(IProcessProfilerEvents eventProcessor)
        {
            EventProcessor = eventProcessor;
        }
    }
}
