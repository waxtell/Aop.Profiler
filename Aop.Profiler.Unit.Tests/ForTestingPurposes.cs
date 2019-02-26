using System.Threading.Tasks;

namespace Aop.Profiler.Unit.Tests
{
    public interface IForTestingPurposes
    {
        string MethodCall(int arg1, string arg2);

        string Member { get; set; }

        string VirtualMethodCall(int arg1, string arg2);

        Task AsyncAction(int arg1, string arg2);
        void SynchronousAction(int arg1, int arg2, string arg3);

        Task<string> AsyncMethodCall(int arg1, string arg2);
    }

    public class ForTestingPurposes : IForTestingPurposes
    {
        public string Member { get; set; }

        public string MethodCall(int arg1, string arg2)
        {
            return arg1 + arg2;
        }

        public virtual string VirtualMethodCall(int arg1, string arg2)
        {
            return arg1 + arg2;
        }

        public async Task AsyncAction(int arg1, string arg2)
        {
            await Task.Delay(0);
        }

        public void SynchronousAction(int arg1, int arg2, string arg3)
        {
        }

        public async Task<string> AsyncMethodCall(int arg1, string arg2)
        {
            await Task.Delay(0);

            return arg1 + arg2;
        }
    }
 }