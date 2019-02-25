using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aop.Profiler.EventProcessing;
using Newtonsoft.Json;
using Xunit;

namespace Aop.Profiler.Unit.Tests
{
    public class PerInstanceAdapterTests
    {
        [Fact]
        public async Task SerializedResultInvokedAsyncMethodResult()
        {
            var eventCount = 0;
            object serializedResult = null;

            var proxy = new PerInstanceAdapter<IForTestingPurposes>
            (
                new ForTestingPurposes(),
                Process.Lean(EventProcessor),
                CaptureOptions.SerializedResult
            ).Object;

            var result = await proxy.AsyncMethodCall(1, "one");

            Thread.Sleep(1000);

            Assert.Equal(1, eventCount);
            Assert.Equal(result, JsonConvert.DeserializeObject((string)serializedResult));

            void EventProcessor(IDictionary<string, object> @event)
            {
                eventCount++;
                serializedResult = @event[nameof(CaptureOptions.SerializedResult)];
            }
        }

        [Fact]
        public void MethodNameMatchesInvokedMethod()
        {
            var eventCount = 0;
            object methodName = null;

            var proxy = new PerInstanceAdapter<IForTestingPurposes>
            (
                new ForTestingPurposes(),
                Process.Lean(EventProcessor),
                CaptureOptions.MethodName
            ).Object;

            proxy.MethodCall(0, "zero");

            Assert.Equal(1, eventCount);
            Assert.Equal(nameof(ForTestingPurposes.MethodCall),methodName);

            void EventProcessor(IDictionary<string, object> @event)
            {
                eventCount++;
                methodName = @event[nameof(CaptureOptions.MethodName)];
            }
        }

        [Fact]
        public void DeclaringTypeMatchesInvokedMethod()
        {
            var eventCount = 0;
            object typeName = null;

            var proxy = new PerInstanceAdapter<IForTestingPurposes>
            (
                new ForTestingPurposes(),
                Process.Lean(EventProcessor),
                CaptureOptions.DeclaringTypeName
            ).Object;

            proxy.MethodCall(0, "zero");

            Assert.Equal(1, eventCount);
            Assert.Equal(typeof(ForTestingPurposes).FullName, typeName);

            void EventProcessor(IDictionary<string, object> @event)
            {
                eventCount++;
                typeName = @event[nameof(CaptureOptions.DeclaringTypeName)];
            }
        }

        [Fact]
        public void SerializedResultInvokedMethodResult()
        {
            var eventCount = 0;
            object serializedResult = null;

            var proxy = new PerInstanceAdapter<IForTestingPurposes>
            (
                new ForTestingPurposes(),
                Process.Lean(EventProcessor),
                CaptureOptions.SerializedResult
            ).Object;

            var result = proxy.MethodCall(0, "zero");

            Assert.Equal(1, eventCount);
            Assert.Equal(result, JsonConvert.DeserializeObject((string)serializedResult));

            void EventProcessor(IDictionary<string, object> @event)
            {
                eventCount++;
                serializedResult = @event[nameof(CaptureOptions.SerializedResult)];
            }
        }

        [Fact]
        public void SerializedParametersDoesNotThrowForPropertyGetter()
        {
            var eventCount = 0;
            object serializedInput = null;

            var proxy = new PerInstanceAdapter<IForTestingPurposes>
            (
                new ForTestingPurposes(),
                Process.Lean(EventProcessor),
                CaptureOptions.SerializedInputParameters
            ).Object;

            var _ = proxy.Member;

            Assert.Equal(1, eventCount);
            Assert.Equal("[]", serializedInput);

            void EventProcessor(IDictionary<string, object> @event)
            {
                eventCount++;
                serializedInput = @event[nameof(CaptureOptions.SerializedInputParameters)];
            }
        }

        [Fact]
        public async Task DefaultOptionsYieldExpectedDictionarySize()
        {
            var eventCount = 0;
            var itemCount = 0;

            var proxy = new PerInstanceAdapter<IForTestingPurposes>
            (
                new ForTestingPurposes(),
                Process.Lean(EventProcessor)
            ).Object;

            await proxy.AsyncAction(0, "zero");

            Thread.Sleep(1000);

            Assert.Equal(1, eventCount);
            Assert.Equal(BitCount((int) CaptureOptions.Default), itemCount);

            int BitCount(int n)
            {
                var test = n;
                var count = 0;

                while (test != 0)
                {
                    if ((test & 1) == 1)
                    {
                        count++;
                    }
                    test >>= 1;
                }

                return count;
            }

            void EventProcessor(IDictionary<string, object> @event)
            {
                eventCount++;
                itemCount = @event.Count;
            }
        }
    }
}
