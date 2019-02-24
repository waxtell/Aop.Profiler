namespace Aop.Profiler
{
    public interface IPerInstanceAdapter<out T> where T : class
    {
        T Object { get; }
    }
}