﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aop.Profiler.EventProcessing;
using Newtonsoft.Json;
using Xunit;

namespace Aop.Profiler.Unit.Tests
{
    public class PerMethodAdapterTests
    {
        [Fact]
        public void NonProfiledMethodDoesNotProfile()
        {
            var eventCount = 0;

            var proxy = new PerMethodAdapter<IForTestingPurposes>
                            (
                                Process.Lean(EventProcessor)
                            )
                            .Adapt(new ForTestingPurposes());

            proxy.MethodCall(0, "zero");

            Assert.Equal(0, eventCount);

            void EventProcessor(IDictionary<string, object> @event)
            {
                eventCount++;
            }
        }

        [Fact]
        public void MethodNameMatchesInvokedMethod()
        {
            var eventCount = 0;
            object methodName = null;

            var proxy = new PerMethodAdapter<IForTestingPurposes>
                            (
                                Process.Lean(EventProcessor)
                            )
                            .Profile(x => x.MethodCall(0,"zero"), CaptureOptions.MethodName)
                            .Adapt(new ForTestingPurposes());


            proxy.MethodCall(0, "zero");

            Assert.Equal(1, eventCount);
            Assert.Equal(nameof(ForTestingPurposes.MethodCall), methodName);

            void EventProcessor(IDictionary<string, object> @event)
            {
                eventCount++;
                methodName = @event[nameof(CaptureOptions.MethodName)];
            }
        }

        [Fact]
        public void SerializedResultMatchesFuzzyInvokedMethodResult()
        {
            var eventCount = 0;
            object serializedResult = null;

            var proxy = new PerMethodAdapter<IForTestingPurposes>
                            (
                                Process.Lean(EventProcessor)
                            )
                            .Profile(x => x.MethodCall(It.IsAny<int>(), It.IsAny<string>()), CaptureOptions.SerializedResult)
                            .Adapt(new ForTestingPurposes());

            var result = proxy.MethodCall(7, "eight");

            Assert.Equal(1, eventCount);
            Assert.Equal(result, JsonConvert.DeserializeObject((string)serializedResult));

            void EventProcessor(IDictionary<string, object> @event)
            {
                eventCount++;
                serializedResult = @event[nameof(CaptureOptions.SerializedResult)];

            }
        }

        [Fact]
        public void DeclaringTypeMatchesInvokedMethod()
        {
            var eventCount = 0;
            object typeName = null;

            var proxy = new PerMethodAdapter<IForTestingPurposes>
                            (
                                Process.Lean(EventProcessor)
                            )
                            .Profile(x => x.MethodCall(0, "zero"), CaptureOptions.DeclaringTypeName)
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
        public async Task SerializedResultInvokedAsyncMethodResult()
        {
            var eventCount = 0;
            object serializedResult = null;

            var proxy = new PerMethodAdapter<IForTestingPurposes>
                            (
                                Process.Lean(EventProcessor)
                            )
                            .Profile(x => x.AsyncMethodCall(It.IsAny<int>(), It.IsAny<string>()), CaptureOptions.SerializedResult)
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
        public void VerboseOptionsYieldExpectedDictionarySize()
        {
            var eventCount = 0;
            var itemCount = 0;

            var proxy = new PerMethodAdapter<ForTestingPurposes>
                            (
                                Process.Lean(EventProcessor)
                            )
                            .Profile(x => x.VirtualMethodCall(It.IsAny<int>(), It.IsAny<string>()), CaptureOptions.Verbose)
                            .Adapt(new ForTestingPurposes());

            proxy.VirtualMethodCall(0, "zero");

            Assert.Equal(1, eventCount);
            Assert.Equal(BitFunctions.CountSet((int)CaptureOptions.Verbose), itemCount);

            void EventProcessor(IDictionary<string, object> @event)
            {
                eventCount++;
                itemCount = @event.Count;
            }
        }

        [Fact]
        public void DefaultOptionsYieldExpectedDictionarySize()
        {
            var eventCount = 0;
            var itemCount = 0;

            var proxy = new PerMethodAdapter<ForTestingPurposes>
                (
                    Process.Lean(EventProcessor)
                )
                .Profile(x => x.VirtualMethodCall(It.IsAny<int>(), It.IsAny<string>()))
                .Adapt(new ForTestingPurposes());

            proxy.VirtualMethodCall(0, "zero");

            Assert.Equal(1, eventCount);
            Assert.Equal(BitFunctions.CountSet((int)CaptureOptions.Default), itemCount);

            void EventProcessor(IDictionary<string, object> @event)
            {
                eventCount++;
                itemCount = @event.Count;
            }
        }

        [Fact]
        public async Task AsyncActionDoesNotAllowSerializedResultOption()
        {
            var eventCount = 0;
            var includesResult = true;

            var proxy = new PerMethodAdapter<IForTestingPurposes>
                (
                    Process.Lean(EventProcessor)
                )
                .Profile(x => x.AsyncAction(It.IsAny<int>(), It.IsAny<string>()), CaptureOptions.SerializedResult)
                .Adapt(new ForTestingPurposes());

            await proxy.AsyncAction(0, "zero");

            Thread.Sleep(1000);

            Assert.Equal(1, eventCount);
            Assert.False(includesResult);

            void EventProcessor(IDictionary<string, object> @event)
            {
                eventCount++;
                includesResult = @event.ContainsKey(nameof(CaptureOptions.SerializedResult));
            }
        }

        [Fact]
        public void SynchronousActionDoesNotAllowSerializedResultOption()
        {
            var eventCount = 0;
            var includesResult = true;

            var proxy = new PerMethodAdapter<IForTestingPurposes>
                            (
                                Process.Lean(EventProcessor)
                            )
                            .Profile(x => x.SynchronousAction(It.IsAny<int>(), It.IsAny<int>(),It.IsNotNull<string>()), CaptureOptions.SerializedResult)
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

        [Fact]
        public void SerializedParametersDoesNotThrowForPropertyGetter()
        {
            var eventCount = 0;
            object serializedInput = null;

            var proxy = new PerMethodAdapter<IForTestingPurposes>
                            (
                                Process.Lean(EventProcessor)
                            )
                            .Profile(x => x.Member, CaptureOptions.SerializedInputParameters)
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
    }
}