using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Aop.Profiler.EventProcessing;

namespace Aop.Profiler
{
    public class PerMethodAdapter<T> : BaseAdapter<T>, IPerMethodAdapter<T> where T : class
    {
        public IPerMethodAdapter<T> Profile<TReturn>(Expression<Func<T, Task<TReturn>>> target, CaptureOptions captureOptions = CaptureOptions.Default)
        {
            return
                Profile
                (
                    target,
                    captureOptions,
                    BuildDelegateForAsynchronousFunc<TReturn>()
                );
        }

        public IPerMethodAdapter<T> Profile(Expression<Func<T, Task>> target, CaptureOptions captureOptions = CaptureOptions.Default)
        {
            return
                Profile
                (
                    target,
                    captureOptions,
                    BuildDelegateForAsynchronousAction()
                );
        }

        public IPerMethodAdapter<T> Profile(Expression<Action<T>> target, CaptureOptions captureOptions = CaptureOptions.Default)
        {
            Profile
            (
                target.Body as MethodCallExpression, 
                captureOptions,
                BuildDelegateForSynchronousAction()
            );

            return this;
        }

        public IPerMethodAdapter<T> Profile<TReturn>(Expression<Func<T, TReturn>> target, CaptureOptions captureOptions = CaptureOptions.Default)
        {
            return
                Profile
                (
                    target,
                    captureOptions,
                    BuildDelegateForSynchronousFunc()
                );
        }

        private void Profile
            (
                MethodCallExpression expression,
                CaptureOptions captureOptions,
                Action<CaptureOptions, DateTime, IInvocation> enqueueAction
            )
        {
            Expectations
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
            Expectations
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

        public override void Intercept(IInvocation invocation)
        {
            var (expectation, enqueue) = Expectations.FirstOrDefault(x => x.expectation.IsHit(invocation));

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

        public PerMethodAdapter(IProcessProfilerEvents eventProcessor) : base(eventProcessor)
        {
        }
    }
}
