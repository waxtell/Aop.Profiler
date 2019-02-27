# Aop.Profiler
Aspect Oriented Performance Profiler.  Supports synchronous and asynchronous actions and functions.

[![Build status](https://ci.appveyor.com/api/projects/status/t2wcqmeu9yuyjq5r?svg=true)](https://ci.appveyor.com/project/waxtell/aop-profiler) [![NuGet Badge](https://buildstats.info/nuget/Aop.Profiler)](https://www.nuget.org/packages/Aop.Profiler/) [![Coverage Status](https://coveralls.io/repos/github/waxtell/Aop.Profiler/badge.svg?branch=master)](https://coveralls.io/github/waxtell/Aop.Profiler?branch=master)

**Explicit Parameter Matching**

``` csharp
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
```

**Fuzzy Parameter Matching**

``` csharp
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
```

**Per Instance (All actions/methods profiled)**

``` csharp
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
```