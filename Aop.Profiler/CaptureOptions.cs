using System;

namespace Aop.Profiler
{
    [Flags]
    public enum CaptureOptions
    {
        StartDateTimeUtc = 1,
        EndDateTimeUtc = 2,
        Duration = 4,
        SerializedInputParameters = 8,
        SerializedResult = 16,
        ThreadId = 32,
        MethodName = 64,
        DeclaringTypeName = 128,
        Default = StartDateTimeUtc | EndDateTimeUtc | Duration | MethodName | DeclaringTypeName
    }
}