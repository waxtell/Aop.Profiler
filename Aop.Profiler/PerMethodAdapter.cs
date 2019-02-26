using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
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
                    captureOptions & ~CaptureOptions.SerializedResult,
                    BuildAsyncEnqueueAction()
                );
        }

        public IPerMethodAdapter<T> Profile(Expression<Action<T>> target, CaptureOptions captureOptions)
        {
            Profile
            (
                target.Body as MethodCallExpression, 
                captureOptions & ~CaptureOptions.SerializedResult,
                BuildSynchronousActionEnqueueAction()
            );

            return this;
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

        private Action<CaptureOptions, DateTime, IInvocation> BuildSynchronousActionEnqueueAction()
        {
            Expression
                <
                    Action<CaptureOptions, DateTime, IInvocation>
                >
                expr =
                    (captureOptions, start, invocation) => Enqueue(captureOptions, start, DateTime.UtcNow, invocation, null);

            return expr.Compile();
        }


        private void Enqueue(CaptureOptions captureOptions, DateTime startUtc, DateTime endUtc, IInvocation invocation, object returnValue)
        {
            try
            {
                var @event = EventFactory.Create(captureOptions, startUtc, endUtc, invocation, returnValue);

                _eventProcessor.ProcessEvent(@event);
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

            if (expectation != null)
            {
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
            else
            {
                invocation.Proceed();
            }
        }

        public PerMethodAdapter(T instance, IProcessProfilerEvents eventProcessor)
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
