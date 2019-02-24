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
                                    Action<CaptureOptions, DateTime, IInvocation>
                                )
                            >
                            _expectations = new List
                            <
                                (
                                    Expectation,
                                    Action<CaptureOptions, DateTime, IInvocation>
                                )
                            >();

        private void Enqueue(CaptureOptions captureOptions, DateTime startUtc, DateTime endUtc, IInvocation invocation)
        {
            try
            {
                var profilerEvent = new Dictionary<string, object>();

                if (captureOptions.HasFlag(CaptureOptions.ThreadId))
                {
                    profilerEvent.Add(nameof(CaptureOptions.ThreadId), Thread.CurrentThread.ManagedThreadId);
                }

                if (captureOptions.HasFlag(CaptureOptions.StartDateTimeUtc))
                {
                    profilerEvent.Add(nameof(CaptureOptions.StartDateTimeUtc), startUtc);
                }

                if (captureOptions.HasFlag(CaptureOptions.EndDateTimeUtc))
                {
                    profilerEvent.Add(nameof(CaptureOptions.EndDateTimeUtc),endUtc);
                }

                if (captureOptions.HasFlag(CaptureOptions.Duration))
                {
                    profilerEvent.Add(nameof(CaptureOptions.Duration), endUtc.Subtract(startUtc));
                }

                if (captureOptions.HasFlag(CaptureOptions.SerializedInputParameters))
                {
                    profilerEvent.Add(nameof(CaptureOptions.SerializedInputParameters), JsonConvert.SerializeObject(invocation.Arguments));
                }

                if (captureOptions.HasFlag(CaptureOptions.MethodName))
                {
                    profilerEvent.Add(nameof(CaptureOptions.MethodName),invocation.Method.Name);
                }

                if (captureOptions.HasFlag(CaptureOptions.SerializedResult))
                {
                    profilerEvent.Add(nameof(CaptureOptions.SerializedResult), JsonConvert.SerializeObject(invocation.ReturnValue));
                }

                _eventProcessor.ProcessEvent(profilerEvent);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
        }

        private Action<CaptureOptions, DateTime, IInvocation> BuildAsyncResultMarshaller<TReturn>()
        {
            Expression
            <
                Action<CaptureOptions, DateTime, IInvocation>
            >
            expr =
                (captureOptions, start, invocation) => (invocation.ReturnValue as Task<TReturn>)
                                                                .ContinueWith
                                                                (
                                                                    i => Enqueue(captureOptions, start,DateTime.UtcNow,invocation)
                                                                );

            return expr.Compile();
        }

        private Action<CaptureOptions, DateTime, IInvocation> BuildAsyncMarshaller()
        {
            Expression
                <
                    Action<CaptureOptions, DateTime, IInvocation>
                >
                expr =
                    (captureOptions, start, invocation) => (invocation.ReturnValue as Task)
                        .ContinueWith
                        (
                            i => Enqueue(captureOptions, start, DateTime.UtcNow, invocation)
                        );

            return expr.Compile();
        }

        private Action<CaptureOptions, DateTime, IInvocation> BuildSynchronousResultMarshaller()
        {
            Expression
                <
                    Action<CaptureOptions, DateTime, IInvocation>
                >
                expr =
                    (captureOptions, start, invocation) => Enqueue(captureOptions, start,DateTime.UtcNow,invocation);

            return expr.Compile();
        }

        public void Intercept(IInvocation invocation)
        {
            var (expectation, enqueue) = _expectations.FirstOrDefault(x => x.expectation.IsHit(invocation));

            if (expectation == null)
            {
                var returnType = invocation.Method.ReturnType;

                expectation = Expectation.FromInvocation(invocation,_captureOptions);
                enqueue = GetMarshallerForType(returnType);

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
                    expectation.Options,
                    start,
                    invocation
                );
        }

        private Action<CaptureOptions, DateTime, IInvocation> GetMarshallerForType(Type tReturn)
        {
            if (tReturn == typeof(Task))
            {
                return BuildAsyncMarshaller();
            }

            var returnType = tReturn?.GetTypeInfo();

            if (returnType != null &&returnType.IsGenericType)
            {
                var gt = returnType.GetGenericTypeDefinition();

                if (gt == typeof(Task<>))
                {
                    return BuildAsyncResultMarshallerForType(returnType.GenericTypeArguments[0]);
                }
            }

            return BuildSynchronousResultMarshaller();
        }

        private Action<CaptureOptions, DateTime, IInvocation> BuildAsyncResultMarshallerForType(Type tReturn)
        {
            var mi = typeof(PerInstanceAdapter<T>)
                        .GetMethod(nameof(BuildAsyncResultMarshaller), BindingFlags.NonPublic | BindingFlags.Instance);

            var miConstructed = mi?.MakeGenericMethod(tReturn);

            return (Action<CaptureOptions, DateTime, IInvocation>)miConstructed?.Invoke(this, null);
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
