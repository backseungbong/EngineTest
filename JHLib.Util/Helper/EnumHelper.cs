using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Helper
{
    public static class EnumDescription<T> where T : unmanaged, Enum
    {
        private readonly static T[] _values;
        private readonly static string[] _descriptions;

        static EnumDescription()
        {
            var values = Enum.GetValues<T>();
            var descriptions = new string[values.Length];

            var count = values.Length;
            if (count != 0)
            {
                do
                {
                    var name = values[--count].ToString();
                    var desc = name;

                    var attrs = typeof(T).GetField(name)?.GetCustomAttributes(typeof(DescriptionAttribute), false);
                    if (attrs != null && attrs.Length != 0 && attrs[0] is DescriptionAttribute attr)
                        desc = attr.Description;

                    descriptions[count] = desc;
                }
                while (count != 0);
            }

            _values = values;
            _descriptions = descriptions;
        }

        public static string[] Descriptions => _descriptions;
        public static T IndexToValue(int index) => (uint)index < (uint)_values.Length ? _values[index] : default;
        public static int ValueToIndex(T value)
        {
            var values = _values;
            if (values.Length != 0)
            {
                var i = 0;
                do if (EqualityComparer<T>.Default.Equals(values[i], value)) return i;
                while (++i < values.Length);
            }
            return 0;
        }
        public static string ValueToDescription(T value)
        {
            var values = _values;
            if (values.Length != 0)
            {
                var i = 0;
                do if (EqualityComparer<T>.Default.Equals(values[i], value)) return _descriptions[i];
                while (++i < values.Length);
            }
            return string.Empty;
        }
    }

    public static class EnumHelper
    {
        private static class EnumT<T> where T : unmanaged, Enum
        {
            private static readonly Dictionary<string, T> _str2val;
            private static readonly Dictionary<T, string> _val2dsc;

            static EnumT()
            {
                var values = Enum.GetValues<T>();
                if (values != null && values.Length != 0)
                {
                    var count = values.Length;
                    var str2val = new Dictionary<string, T>(count * 4);
                    var val2dsc = new Dictionary<T, string>(count);

                    do
                    {
                        var val = values[--count];
                        var name = val.ToString();

                        var attrs = typeof(T).GetField(name)?.GetCustomAttributes(typeof(DescriptionAttribute), false);
                        if (attrs != null && attrs.Length != 0 && attrs[0] is DescriptionAttribute attr)
                        {
                            var dsc = attr.Description.Trim();
                            if (dsc != null && dsc.Length != 0)
                            {
                                str2val.TryAdd(dsc, val);
                                str2val.TryAdd(dsc.ToLowerInvariant(), val);
                                val2dsc.TryAdd(val, dsc);
                            }
                        }
                        str2val.TryAdd(name, val);
                        str2val.TryAdd(name.ToLowerInvariant(), val);
                    }
                    while (count != 0);

                    _str2val = str2val.Count != 0 ? str2val : null;
                    _val2dsc = val2dsc.Count != 0 ? val2dsc : null;
                }
            }

            public static bool IsValid(T value) => _val2dsc?.TryGetValue(value, out _) == true;
            public static string GetDesc(T value)
            {
                var val2dsc = _val2dsc;
                if (val2dsc != null && val2dsc.TryGetValue(value, out var result))
                    return result;
                return null;
            }

            public static T AsEnum(string text, T defaultValue = default) => TryAsEnum(text, out var result) ? result : defaultValue;
            public static bool TryAsEnum(string text, out T result)
            {
                var str2val = _str2val;
                if (str2val != null)
                {
                    var trim = text.Trim();
                    if (str2val.TryGetValue(trim, out result))
                        return true;

                    if (long.TryParse(trim, out var val))
                    {
                        result = Unsafe.As<long, T>(ref val);
                        if (IsValid(result))
                            return true;
                    }

                    if (str2val.TryGetValue(trim.ToLowerInvariant(), out result))
                        return true;
                }

                result = default;
                return false;
            }
        }

        /// <summary> 열거형 값의 Description을 가져온다. Description이 정의가 없을경우 null을 반환한다 </summary>
        public static string Description<T>(this T enumValue) where T : unmanaged, Enum => EnumT<T>.GetDesc(enumValue);

        /// <summary> 열거형 값의 Description을 가져온다. Description이 정의가 없을경우 null을 반환한다 </summary>
        public static string GetDescription<T>(T value) where T : unmanaged, Enum => EnumT<T>.GetDesc(value);

        /// <summary> Text를 통해 열거형 타입으로 가져온다. Text와 매칭되는 값이 없을경우 기본값을 반환한다 </summary>
        public static T AsEnum<T>(string text, T defaultReturn = default) where T : unmanaged, Enum =>
            text != null ? EnumT<T>.AsEnum(text, defaultReturn) : defaultReturn;

        /// <summary> Text를 통해 열거형 타입으로 가져오기 시도한다 </summary>
        public static bool TryAsEnum<T>(string text, out T result) where T : unmanaged, Enum
        {
            if (text != null && EnumT<T>.TryAsEnum(text, out result))
                return true;

            result = default;
            return false;
        }
    }
}