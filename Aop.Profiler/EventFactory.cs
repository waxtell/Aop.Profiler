using System;
using System.Collections.Generic;
using System.Threading;
using Castle.DynamicProxy;
using Newtonsoft.Json;

namespace Aop.Profiler
{
    internal static class EventFactory
    {
        public static IDictionary<string, object> Create(CaptureOptions captureOptions, DateTime startUtc, DateTime endUtc, IInvocation invocation, object returnValue)
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

            return @event;
        }
    }
}
