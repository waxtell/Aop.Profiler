namespace Aop.Profiler
{
    public static class It
    {
        public static T IsAny<T>()
        {
            return default(T);
        }

        public static T IsNotNull<T>()
        {
            return default(T);
        }
    }
}
