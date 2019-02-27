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
                                Process.Lean(EventProcessor),
                                CaptureOptions.SerializedResult
                            )
                            .Adapt(new ForTestingPurposes());

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
                                Process.Lean(EventProcessor),
                                CaptureOptions.MethodName
                            )
                            .Adapt(new ForTestingPurposes());

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
                                Process.Lean(EventProcessor),
                                CaptureOptions.DeclaringTypeName
                            )
                            .Adapt(new ForTestingPurposes());

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
        public void SerializedResultMatchesInvokedMethodResult()
        {
            var eventCount = 0;
            object serializedResult = null;

            var proxy = new PerInstanceAdapter<IForTestingPurposes>
                            (
                                Process.Lean(EventProcessor),
                                CaptureOptions.SerializedResult
                            )
                            .Adapt(new ForTestingPurposes());

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
                                Process.Lean(EventProcessor),
                                CaptureOptions.SerializedInputParameters
                            )
                            .Adapt(new ForTestingPurposes());

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
                                Process.Lean(EventProcessor)
                            )
                            .Adapt(new ForTestingPurposes());


            await proxy.AsyncAction(0, "zero");

            Thread.Sleep(1000);

            Assert.Equal(1, eventCount);
            Assert.Equal(BitFunctions.CountSet((int) CaptureOptions.Default), itemCount);

            void EventProcessor(IDictionary<string, object> @event)
            {
                eventCount++;
                itemCount = @event.Count;
            }
        }

        [Fact]
        public void SynchronousActionDoesNotAllowSerializedResultOption()
        {
            var eventCount = 0;
            var includesResult = true;

            var proxy = new PerInstanceAdapter<IForTestingPurposes>
                            (
                                Process.Lean(EventProcessor),
                                CaptureOptions.SerializedResult
                            )
                            .Adapt(new ForTestingPurposes());

            proxy.SynchronousAction(0, 1, "two");

            Assert.Equal(1, eventCount);
            Assert.False(includesResult);

            void EventProcessor(IDictionary<string, object> @event)
            {
                eventCount++;
                includesResult = @event.ContainsKey(nameof(CaptureOptions.SerializedResult));
            }
        }
    }
}
