namespace Aop.Profiler
{
    public interface IPerMethodAdapter<out T> where T : class
    {
        T Object { get; }
    }
}