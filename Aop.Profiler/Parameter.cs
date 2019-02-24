using Newtonsoft.Json;

namespace Aop.Profiler
{
    internal class Parameter
    {
        private readonly MatchPrecision _precision;
        private readonly string _serializedValue;

        private enum MatchPrecision
        {
            Exact,
            Any,
            NotNull
        }

        private Parameter(string serializedValue, MatchPrecision precision)
        {
            _serializedValue = serializedValue;
            _precision = precision;
        }

        public static Parameter MatchExact(object value)
        {
            return new Parameter(JsonConvert.SerializeObject(value), MatchPrecision.Exact);
        }

        public static Parameter MatchAny()
        {
            return new Parameter(null, MatchPrecision.Any);
        }

        public static Parameter MatchNotNull()
        {
            return new Parameter(null, MatchPrecision.NotNull);
        }

        public bool IsMatch(object value)
        {
            switch (_precision)
            {
                case MatchPrecision.Any:
                    return true;
                case MatchPrecision.NotNull:
                    return value != null;
                default:
                    return JsonConvert.SerializeObject(value) == _serializedValue;
            }
        }
    }
}
