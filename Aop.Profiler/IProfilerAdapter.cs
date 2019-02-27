namespace Aop.Profiler
{
    public interface IProfilerAdapter<T>
    {
        T Adapt(T instance);
    }
}