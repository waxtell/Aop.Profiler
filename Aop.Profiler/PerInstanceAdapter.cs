using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Aop.Profiler.EventProcessing;

namespace Aop.Profiler
{
    using EnqueueDelegate = Action<CaptureOptions, DateTime, IInvocation>;

    public class PerInstanceAdapter<T> : BaseAdapter<T>, IPerInstanceAdapter<T> where T : class
    {
        private readonly CaptureOptions _captureOptions;

        private EnqueueDelegate BuildDelegateForType(Type tReturn)
        {
            if (tReturn == typeof(void))
            {
                return BuildDelegateForSynchronousAction();
            }

            if (tReturn == typeof(Task))
            {
                return BuildDelegateForAsynchronousAction();
            }

            var returnType = tReturn?.GetTypeInfo();

            if (returnType != null &&returnType.IsGenericType)
            {
                var gt = returnType.GetGenericTypeDefinition();

                if (gt == typeof(Task<>))
                {
                    return BuildDelegateForAsynchronousFuncForType(returnType.GenericTypeArguments[0]);
                }
            }

            return BuildDelegateForSynchronousFunc();
        }

        private EnqueueDelegate BuildDelegateForAsynchronousFuncForType(Type tReturn)
        {
            var mi = typeof(PerInstanceAdapter<T>)
                        .GetMethod(nameof(BuildDelegateForAsynchronousFunc), BindingFlags.NonPublic | BindingFlags.Instance);

            var miConstructed = mi?.MakeGenericMethod(tReturn);

            return (EnqueueDelegate)miConstructed?.Invoke(this, null);
        }

        public override void Intercept(IInvocation invocation)
        {
            var (expectation, enqueue) = Expectations.FirstOrDefault(x => x.expectation.IsHit(invocation));

            if (expectation == null)
            {
                var returnType = invocation.Method.ReturnType;

                expectation = Expectation.FromInvocation(invocation, _captureOptions);
                enqueue = BuildDelegateForType(returnType);

                Expectations
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

        public PerInstanceAdapter(T instance, IProcessProfilerEvents eventProcessor, CaptureOptions captureOptions = CaptureOptions.Default)
        : base(instance, eventProcessor)
        {
            _captureOptions = captureOptions;
        }
    }
}
