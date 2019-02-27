namespace Aop.Profiler
{
    public interface IPerInstanceAdapter<T> : IProfilerAdapter<T> where T : class
    {
    }
}