using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Castle.DynamicProxy;

namespace Aop.Profiler
{
    internal class Expectation
    {
        private readonly string _methodName;
        private readonly Type _returnType;
        private readonly IEnumerable<Parameter> _parameters;
        public CaptureOptions Options { get; }

        private Expectation(string methodName, Type returnType, IEnumerable<Parameter> parameters, CaptureOptions captureOptions)
        {
            _methodName = methodName;
            _returnType = returnType;
            _parameters = parameters;
            Options = captureOptions;
        }

        private static Parameter ToParameter(Expression element)
        {
            if (element is ConstantExpression expression)
            {
                return Parameter.MatchExact(expression.Value);
            }

            if (element is MethodCallExpression methodCall)
            {
                if (methodCall.Method.DeclaringType == typeof(It))
                {
                    switch (methodCall.Method.Name)
                    {
                        case nameof(It.IsAny):
                            return Parameter.MatchAny();
                        case nameof(It.IsNotNull):
                            return Parameter.MatchNotNull();
                        default:
                            throw new NotSupportedException("Specified fuzzy match is not supported.");
                    }
                }
            }

            return 
                Parameter
                    .MatchExact
                    (
                        Expression
                            .Lambda(Expression.Convert(element, element.Type))
                            .Compile()
                            .DynamicInvoke()
                    );
        }

        public static Expectation FromMethodCallExpression(MethodCallExpression expression, CaptureOptions captureOptions)
        {
            return new Expectation
            (
                expression.Method.Name,
                expression.Method.ReturnType,
                expression.Arguments.Select(ToParameter).ToArray(),
                captureOptions
            );
        }

        public static Expectation FromMemberAccessExpression(MemberExpression expression, CaptureOptions captureOptions)
        {
            var propertyInfo = (PropertyInfo) expression.Member;

            return new Expectation
            (
                propertyInfo.GetMethod.Name,
                propertyInfo.PropertyType,
                new List<Parameter>(),
                captureOptions
            );
        }

        public static Expectation FromInvocation(IInvocation invocation, CaptureOptions captureOptions)
        {
            return new Expectation
            (
                invocation.MethodInvocationTarget.Name,
                invocation.MethodInvocationTarget.ReturnType,
                invocation.Arguments.Select(Parameter.MatchExact),
                captureOptions
            );
        }

        public bool IsHit(IInvocation invocation)
        {
            return
                IsHit
                (
                    invocation.Method.Name,
                    invocation.Method.ReturnType,
                    invocation.Arguments
                );
        }

        public bool IsHit(string methodName, Type returnType, object[] arguments)
        {
            if (methodName != _methodName || returnType != _returnType)
            {
                return false;
            }

            if (arguments.Length != _parameters.Count())
            {
                return false;
            }

            for (var i = 0; i < arguments.Length; i++)
            {
                if (!_parameters.ElementAt(i).IsMatch(arguments[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
