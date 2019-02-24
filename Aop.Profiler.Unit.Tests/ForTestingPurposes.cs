using System.Threading.Tasks;

namespace Aop.Profiler.Unit.Tests
{
    public interface IForTestingPurposes
    {
        string MethodCall(int arg1, string arg2);

        string Member { get; set; }

        string VirtualMethodCall(int arg1, string arg2);

        Task AsyncAction(int arg1, string arg2);

        Task<string> AsyncMethodCall(int arg1, string arg2);
    }

    public class ForTestingPurposes : IForTestingPurposes
    {
        public uint MemberGetInvocationCount { get; private set; }
        public uint MemberSetInvocationCount { get; private set; }
        public uint MethodCallInvocationCount { get; private set; }
        public uint VirtualMethodCallInvocationCount { get; private set; }
        public uint AsyncMethodCallInvocationCount { get; private set; }

        private string _member;

        public string Member
        {
            get
            {
                MemberGetInvocationCount++;
                return _member;
            }

            set
            {
                MemberSetInvocationCount++;
                _member = value;
            }
        }

        public string MethodCall(int arg1, string arg2)
        {
            MethodCallInvocationCount++;
            return arg1 + arg2;
        }

        public virtual string VirtualMethodCall(int arg1, string arg2)
        {
            VirtualMethodCallInvocationCount++;
            return arg1 + arg2;
        }

        public ForTestingPurposes()
        {
            MemberGetInvocationCount = 0;
            MemberSetInvocationCount = 0;
            MethodCallInvocationCount = 0;
        }

        public async Task AsyncAction(int arg1, string arg2)
        {
            await Task.Delay(0);
        }

        public async Task<string> AsyncMethodCall(int arg1, string arg2)
        {
            AsyncMethodCallInvocationCount++;

            await Task.Delay(0);

            return arg1 + arg2;
        }
    }
 }