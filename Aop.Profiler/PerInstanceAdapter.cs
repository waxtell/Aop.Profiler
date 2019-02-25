using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Newtonsoft.Json;
using Aop.Profiler.EventProcessing;

namespace Aop.Profiler
{
    public class PerInstanceAdapter<T> : IInterceptor, IPerInstanceAdapter<T> where T : class
    {
        private readonly IProcessProfilerEvents _eventProcessor;
        private readonly CaptureOptions _captureOptions;

        public T Object { get; }

        private readonly List
                            <
                                (
                                    Expectation expectation,
                                    Action<DateTime, IInvocation>
                                )
                            >
                            _expectations = new List
                            <
                                (
                                    Expectation,
                                    Action<DateTime, IInvocation>
                                )
                            >();

        private void Enqueue(DateTime startUtc, DateTime endUtc, IInvocation invocation, object returnValue)
        {
            try
            {
                var profilerEvent = new Dictionary<string, object>();

                if (_captureOptions.HasFlag(CaptureOptions.ThreadId))
                {
                    profilerEvent.Add(nameof(CaptureOptions.ThreadId), Thread.CurrentThread.ManagedThreadId);
                }

                if (_captureOptions.HasFlag(CaptureOptions.StartDateTimeUtc))
                {
                    profilerEvent.Add(nameof(CaptureOptions.StartDateTimeUtc), startUtc);
                }

                if (_captureOptions.HasFlag(CaptureOptions.EndDateTimeUtc))
                {
                    profilerEvent.Add(nameof(CaptureOptions.EndDateTimeUtc),endUtc);
                }

                if (_captureOptions.HasFlag(CaptureOptions.Duration))
                {
                    profilerEvent.Add(nameof(CaptureOptions.Duration), endUtc.Subtract(startUtc));
                }

                if (_captureOptions.HasFlag(CaptureOptions.SerializedInputParameters))
                {
                    profilerEvent.Add(nameof(CaptureOptions.SerializedInputParameters), JsonConvert.SerializeObject(invocation.Arguments));
                }

                if (_captureOptions.HasFlag(CaptureOptions.MethodName))
                {
                    profilerEvent.Add(nameof(CaptureOptions.MethodName),invocation.Method.Name);
                }

                if (_captureOptions.HasFlag(CaptureOptions.SerializedResult))
                {
                    profilerEvent.Add(nameof(CaptureOptions.SerializedResult), JsonConvert.SerializeObject(returnValue));
                }

                if (_captureOptions.HasFlag(CaptureOptions.DeclaringTypeName))
                {
                    profilerEvent.Add(nameof(CaptureOptions.DeclaringTypeName), invocation.TargetType.FullName);
                }

                _eventProcessor.ProcessEvent(profilerEvent);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
        }

        private Action<DateTime, IInvocation> BuildAsyncResultEnqueueAction<TReturn>()
        {
            Expression
            <
                Action<DateTime, IInvocation>
            >
            expr =
                (start, invocation) => (invocation.ReturnValue as Task<TReturn>)
                                                                .ContinueWith
                                                                (
                                                                    i => Enqueue(start,DateTime.UtcNow,invocation, i.Result)
                                                                );

            return expr.Compile();
        }

        private Action<DateTime, IInvocation> BuildAsyncEnqueueAction()
        {
            Expression
                <
                    Action<DateTime, IInvocation>
                >
                expr =
                    (start, invocation) => (invocation.ReturnValue as Task)
                        .ContinueWith
                        (
                            i => Enqueue(start, DateTime.UtcNow, invocation, null)
                        );

            return expr.Compile();
        }

        private Action<DateTime, IInvocation> BuildSynchronousResultEnqueueAction()
        {
            Expression
                <
                    Action<DateTime, IInvocation>
                >
                expr =
                    (start, invocation) => Enqueue(start,DateTime.UtcNow,invocation,invocation.ReturnValue);

            return expr.Compile();
        }

        public void Intercept(IInvocation invocation)
        {
            var (expectation, enqueue) = _expectations.FirstOrDefault(x => x.expectation.IsHit(invocation));

            if (expectation == null)
            {
                var returnType = invocation.Method.ReturnType;

                expectation = Expectation.FromInvocation(invocation,_captureOptions);
                enqueue = GetEnqueueActionForType(returnType);

                _expectations
                    .Add
                    (
                        (
                            expectation,
                            enqueue
                        )
                    );
            }

            var start = DateTime.UtcNow;

            invocation.Proceed();

            enqueue
                .Invoke
                (
                    start,
                    invocation
                );
        }

        private Action<DateTime, IInvocation> GetEnqueueActionForType(Type tReturn)
        {
            if (tReturn == typeof(Task))
            {
                return BuildAsyncEnqueueAction();
            }

            var returnType = tReturn?.GetTypeInfo();

            if (returnType != null &&returnType.IsGenericType)
            {
                var gt = returnType.GetGenericTypeDefinition();

                if (gt == typeof(Task<>))
                {
                    return BuildAsyncResultEnqueueActionForType(returnType.GenericTypeArguments[0]);
                }
            }

            return BuildSynchronousResultEnqueueAction();
        }

        private Action<DateTime, IInvocation> BuildAsyncResultEnqueueActionForType(Type tReturn)
        {
            var mi = typeof(PerInstanceAdapter<T>)
                        .GetMethod(nameof(BuildAsyncResultEnqueueAction), BindingFlags.NonPublic | BindingFlags.Instance);

            var miConstructed = mi?.MakeGenericMethod(tReturn);

            return (Action<DateTime, IInvocation>)miConstructed?.Invoke(this, null);
        }

        public PerInstanceAdapter(T instance, IProcessProfilerEvents eventProcessor, CaptureOptions captureOptions = CaptureOptions.Default)
        {
            _captureOptions = captureOptions;
            _eventProcessor = eventProcessor;

            if (typeof(T).GetTypeInfo().IsInterface)
            {
                Object = new ProxyGenerator()
                    .CreateInterfaceProxyWithTarget(instance, this);
            }
            else
            {
                Object = new ProxyGenerator()
                    .CreateClassProxyWithTarget(instance, this);
            }
        }
    }
}
