namespace Aop.Profiler.Unit.Tests
{
    internal static class BitFunctions
    {
        public static int CountSet(int n)
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
    }
}
