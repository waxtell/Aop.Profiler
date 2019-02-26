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
    public class PerMethodAdapter<T> : IInterceptor, IPerMethodAdapter<T> where T : class
    {
        private readonly IProcessProfilerEvents _eventProcessor;

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

        public IPerMethodAdapter<T> Profile<TReturn>(Expression<Func<T, Task<TReturn>>> target, CaptureOptions captureOptions)
        {
            return
                Profile
                (
                    target,
                    captureOptions,
                    BuildAsyncResultEnqueueAction<TReturn>()
                );
        }
        public IPerMethodAdapter<T> Profile(Expression<Func<T, Task>> target, CaptureOptions captureOptions)
        {
            return
                Profile
                (
                    target,
                    captureOptions,
                    BuildAsyncEnqueueAction()
                );
        }

        public IPerMethodAdapter<T> Profile<TReturn>(Expression<Func<T, TReturn>> target, CaptureOptions captureOptions)
        {
            return
                Profile
                (
                    target,
                    captureOptions,
                    BuildSynchronousResultEnqueueAction()
                );
        }

        private void Profile
            (
                MethodCallExpression expression,
                CaptureOptions captureOptions,
                Action<CaptureOptions, DateTime, IInvocation> enqueueAction
            )
        {
            _expectations
                .Add
                (
                    (
                        Expectation
                            .FromMethodCallExpression
                            (
                                expression,
                                captureOptions
                            ),
                        enqueueAction
                    )
                );
        }

        private void Profile
            (
                MemberExpression expression,
                CaptureOptions captureOptions,
                Action<CaptureOptions, DateTime, IInvocation> enqueueAction
            )
        {
            _expectations
                .Add
                (
                    (
                        Expectation
                            .FromMemberAccessExpression
                            (
                                expression,
                                captureOptions
                            ),
                        enqueueAction
                    )
                );
        }

        private IPerMethodAdapter<T> Profile<TReturn>
            (
                Expression<Func<T, TReturn>> target,
                CaptureOptions captureOptions,
                Action<CaptureOptions, DateTime, IInvocation> enqueueAction
            )
        {
            MethodCallExpression expression = null;

            switch (target.Body)
            {
                case MemberExpression memberExpression:
                    Profile(memberExpression, captureOptions, enqueueAction);
                    return this;

                case UnaryExpression unaryExpression:
                    expression = unaryExpression.Operand as MethodCallExpression;
                    break;
            }

            expression = expression ?? target.Body as MethodCallExpression;

            Profile(expression, captureOptions, enqueueAction);

            return this;
        }

        private Action<CaptureOptions, DateTime, IInvocation> BuildAsyncEnqueueAction()
        {
            Expression
                <
                    Action<CaptureOptions, DateTime, IInvocation>
                >
                expr =
                    (captureOptions, start, invocation) => (invocation.ReturnValue as Task)
                        .ContinueWith
                        (
                            i => Enqueue(captureOptions, start, DateTime.UtcNow, invocation, null)
                        );

            return expr.Compile();
        }

        private void Enqueue(CaptureOptions captureOptions, DateTime startUtc, DateTime endUtc, IInvocation invocation, object returnValue)
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
                    profilerEvent.Add(nameof(CaptureOptions.EndDateTimeUtc), endUtc);
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
                    profilerEvent.Add(nameof(CaptureOptions.MethodName), invocation.Method.Name);
                }

                if (captureOptions.HasFlag(CaptureOptions.SerializedResult))
                {
                    profilerEvent.Add(nameof(CaptureOptions.SerializedResult), JsonConvert.SerializeObject(returnValue));
                }

                if (captureOptions.HasFlag(CaptureOptions.DeclaringTypeName))
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

        private Action<CaptureOptions, DateTime, IInvocation> BuildAsyncResultEnqueueAction<TReturn>()
        {
            Expression
            <
                Action<CaptureOptions, DateTime, IInvocation>
            >
            expr =
                (captureOptions, start, invocation) => (invocation.ReturnValue as Task<TReturn>)
                                                                .ContinueWith
                                                                (
                                                                    i => Enqueue(captureOptions, start,DateTime.UtcNow,invocation, i.Result)
                                                                );

            return expr.Compile();
        }

        private Action<CaptureOptions, DateTime, IInvocation> BuildSynchronousResultEnqueueAction()
        {
            Expression
                <
                    Action<CaptureOptions, DateTime, IInvocation>
                >
                expr =
                    (captureOptions, start, invocation) => Enqueue(captureOptions, start,DateTime.UtcNow,invocation,invocation.ReturnValue);

            return expr.Compile();
        }

        public void Intercept(IInvocation invocation)
        {
            var (expectation, enqueue) = _expectations.FirstOrDefault(x => x.expectation.IsHit(invocation));

            //if (expectation == null)
            //{
            //    var returnType = invocation.Method.ReturnType;

            //    expectation = Expectation.FromInvocation(invocation,_captureOptions);
            //    enqueue = GetEnqueueActionForType(returnType);

            //    _expectations
            //        .Add
            //        (
            //            (
            //                expectation,
            //                enqueue
            //            )
            //        );
            //}

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

        public PerMethodAdapter(T instance, IProcessProfilerEvents eventProcessor, CaptureOptions captureOptions = CaptureOptions.Default)
        {
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
