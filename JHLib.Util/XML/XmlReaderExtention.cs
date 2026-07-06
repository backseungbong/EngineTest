using JHLib.Util.Helper;
using JHLib.Util.List;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Xml;

namespace JHLib.Util.XML
{
    public static class XmlReaderExtention
    {
        private static readonly XmlReaderSettings IgnoreSettings = new()
        {
            IgnoreWhitespace = true,
            IgnoreComments = true,
        };

        public static XmlReader Create(string path) =>
            XmlReader.Create(path, IgnoreSettings);

        /// <summary> 다음 위치의 가장 가까운 엘리먼트로 이동해 로컬 네임을 반환한다 </summary>
        public static bool NextElementName(this XmlReader xr, out string localName)
        {
            if (xr.Read())
            {
                do
                {
                    if (xr.NodeType == XmlNodeType.Element)
                    {
                        localName = xr.LocalName;
                        return true;
                    }
                }
                while (xr.Read());
            }
            Unsafe.SkipInit(out localName);
            return false;
        }

        /// <summary> 현재 위치의 지정 깊이에서 가장 가까운 엘리먼트로 이동해 로컬 네임을 반환한다 </summary>
        public static bool MoveElementName(this XmlReader xr, int targetDepth, out string localName)
        {
            do
            {
                var depth = xr.Depth;
                if (depth < targetDepth) { break; }
                if (depth > targetDepth) { continue; }
                if (xr.NodeType == XmlNodeType.Element)
                {
                    localName = xr.LocalName;
                    return true;
                }
            }
            while (xr.Read());
            Unsafe.SkipInit(out localName);
            return false;
        }

        /// <summary> 현재 위치의 지정 깊이에서 가장 가까운 엘리먼트로 이동해 로컬 네임을 체크한다 </summary>
        public static bool CheckElementName(this XmlReader xr, int targetDepth, string localName)
        {
            do
            {
                var depth = xr.Depth;
                if (depth < targetDepth) { break; }
                if (depth > targetDepth) { continue; }
                if (xr.NodeType == XmlNodeType.Element)
                {
                    if (localName != xr.LocalName) { break; }
                    return true;
                }
            }
            while (xr.Read());
            return false;
        }

        public static XREnumerator Enumerate(this XmlReader xr, string localName = null) =>
            new(xr, localName, false);
        public static XREnumerator EnumerateChildren(this XmlReader xr, string localName = null) =>
            new(xr, localName, true);

        public static void ActionMap<T>(this XmlReader xr, Dictionary<string, Action<XmlReader, T>> actionMap, T item)
        {
            var depth = xr.Depth + 1;
            if (xr.Read())
            {
                while (MoveElementName(xr, depth, out var local))
                {
                    if (actionMap.TryGetValue(local, out var action))
                    {
                        action(xr, item);
                    }
                    else
                    {
                        Trace.WriteLine($"Unknown XmlElement : {local}");
                        xr.Read();
                    }
                }
            }
        }


        // ========================= GetValue =========================
        public static bool NextValue(this XmlReader xr, out bool result, bool defualtValue = false)
        {
            if (xr.Read() && AsBool(xr.Value, out var r))
            {
                result = r;
                return true;
            }
            else
            {
                result = defualtValue;
                return false;
            }
        }
        public static bool NextValue(this XmlReader xr, out string result, string defualtValue = null)
        {
            if (xr.Read())
            {
                result = xr.Value;
                return true;
            }
            else
            {
                result = defualtValue;
                return false;
            }
        }
        public static bool NextValue<T>(this XmlReader xr, out T result, T defualtValue = default) where T : INumber<T>
        {
            if (xr.Read() && T.TryParse(xr.Value, CultureInfo.InvariantCulture, out var r))
            {
                result = r;
                return true;
            }
            else
            {
                result = defualtValue;
                return false;
            }
        }
        public static bool NextValue<T>(this XmlReader xr, out T? result, T? defualtValue = default) where T : struct, INumber<T>
        {
            if (xr.Read() && T.TryParse(xr.Value, CultureInfo.InvariantCulture, out var r))
            {
                result = r;
                return true;
            }
            else
            {
                result = defualtValue;
                return false;
            }
        }

        public static bool NextValues(this XmlReader xr, string localName, out string[] result, string[] defualtValue = null)
        {
            var depth = xr.Depth;
            var list = new FList<string>(4);
            do
            {
                if (xr.Read()) { list.Add(xr.Value); }
                else { break; }
            }
            while (CheckElementName(xr, depth, localName));

            if (list.Count != 0)
            {
                result = list.ToArray();
                return true;
            }
            else
            {
                result = defualtValue;
                return false;
            }
        }

        public static bool NextValues<T>(this XmlReader xr, string localName, out T[] result, T[] defualtValue = null) where T : INumber<T>
        {
            var depth = xr.Depth;
            var list = new FList<T>(4);
            do
            {
                if (xr.NextValue(out T value)) { list.Add(value); }
                else { break; }
            }
            while (CheckElementName(xr, depth, localName));

            if (list.Count != 0)
            {
                result = list.ToArray();
                return true;
            }
            else
            {
                result = defualtValue;
                return false;
            }
        }

        public static bool NextValueAsEnum<T>(this XmlReader xr, out T result) where T : unmanaged, Enum
        {
            if (xr.Read() && EnumHelper.TryAsEnum(xr.Value, out result))
                return true;

            result = default;
            return false;
        }

        // ========================= Attribute =========================

        /// <summary> 
        /// 다음 속성위치로 이동을 시도하고 속성 네임을 반환한다 <para/>
        /// 속성이 없거나 모든 속성을 읽은경우 false를 반환하고 다음 위치로 이동한다
        /// </summary>
        public static bool NextAttrName(this XmlReader xr, out string localName)
        {
            Unsafe.SkipInit(out localName);
            if (xr.MoveToNextAttribute())
            {
                localName = xr.LocalName;
                return true;
            }
            xr.Read();
            return false;
        }

        /// <summary> 
        /// 다음 속성위치로 이동을 시도하고 속성 값을 반환한다 <para/>
        /// 속성이 없거나 모든 속성을 읽은경우 false를 반환하고 다음 위치로 이동한다
        /// </summary>
        public static bool NextAttrValue(this XmlReader xr, out string result)
        {
            Unsafe.SkipInit(out result);
            if (xr.MoveToNextAttribute())
            {
                result = xr.Value;
                return true;
            }
            xr.Read();
            return false;
        }

        /// <summary> 
        /// 다음 속성위치로 이동을 시도하고 속성 값을 반환한다 <para/>
        /// 속성이 없거나 모든 속성을 읽은경우 false를 반환하고 다음 위치로 이동한다
        /// </summary>
        public static bool NextAttrValue<T>(this XmlReader xr, out T result) where T : INumber<T>
        {
            Unsafe.SkipInit(out result);
            if (xr.MoveToNextAttribute())
            {
                result = ValueAsNumber<T>(xr);
                return true;
            }
            xr.Read();
            return false;
        }


        /// <summary> 지정된 이름과 동일한 모든 엘리먼트의 첫번재 속성들을 읽어온다 </summary>
        public static bool NextAttrValues(this XmlReader xr, string localName, out string[] results, string[] defaultValue = null)
        {
            var depth = xr.Depth;
            var list = new FList<string>(4);
            do if (NextAttrValue(xr, out var value)) { list.Add(value); }
            while (CheckElementName(xr, depth, localName));

            if (list.Count != 0)
            {
                results = list.ToArray();
                return true;
            }
            else
            {
                results = defaultValue;
                return false;
            }
        }

        /// <summary> 지정된 이름과 동일한 모든 엘리먼트의 첫번재 속성들을 읽어온다 </summary>
        public static bool NextAttrValues<T>(this XmlReader xr, string localName, out T[] results, T[] defaultValue = null) where T : INumber<T>
        {
            var depth = xr.Depth;
            var list = new FList<T>(4);
            do if (NextAttrValue(xr, out T value)) { list.Add(value); }
            while (CheckElementName(xr, depth, localName));

            if (list.Count != 0)
            {
                results = list.ToArray();
                return true;
            }
            else
            {
                results = defaultValue;
                return false;
            }
        }

        /// <summary> 특정 속성명의 속성값을 반환하고 엘리먼트 위치로 복귀한다 </summary>
        public static bool GetAttrValue(this XmlReader xr, string localName, out string result, string defaultValue = null)
        {
            if (xr.MoveToFirstAttribute())
            {
                do
                {
                    if (xr.LocalName == localName)
                    {
                        result = xr.Value;
                        xr.MoveToElement();
                        return true;
                    }
                }
                while (xr.MoveToNextAttribute());
                xr.MoveToElement();
            }
            result = defaultValue;
            return false;
        }
        /// <summary> 특정 속성명의 속성값을 반환하고 엘리먼트 위치로 복귀한다 </summary>
        public static bool GetAttrValue(this XmlReader xr, string localName, out bool result, bool defaultValue = false)
        {
            if (GetAttrValue(xr, localName, out string str) && AsBool(str, out var r))
            {
                result = r;
                return true;
            }
            else
            {
                result = defaultValue;
                return false;
            }
        }
        /// <summary> 특정 속성명의 속성값을 반환하고 엘리먼트 위치로 복귀한다 </summary>
        public static bool GetAttrValue<T>(this XmlReader xr, string localName, out T result, T defaultValue = default) where T : INumber<T>
        {
            if (GetAttrValue(xr, localName, out string str) && T.TryParse(str, CultureInfo.InvariantCulture, out var r))
            {
                result = r;
                return true;
            }
            else
            {
                result = defaultValue;
                return false;
            }
        }
        /// <summary> 특정 속성명의 속성값을 반환하고 엘리먼트 위치로 복귀한다 </summary>
        public static bool GetAttrValue<T>(this XmlReader xr, string localName, out T? result, T? defaultValue = default) where T : struct, INumber<T>
        {
            if (GetAttrValue(xr, localName, out string str) && T.TryParse(str, CultureInfo.InvariantCulture, out var r))
            {
                result = r;
                return true;
            }
            else
            {
                result = defaultValue;
                return false;
            }
        }
        /// <summary> 특정 속성명의 속성값을 반환하고 엘리먼트 위치로 복귀한다 </summary>
        public static bool GetAttrValueAsEnum<T>(this XmlReader xr, string localName, out T result, T defaultValue = default) where T : unmanaged, Enum
        {
            if (xr.MoveToFirstAttribute())
            {
                do
                {
                    if (xr.LocalName == localName)
                    {
                        if (EnumHelper.TryAsEnum(xr.Value, out result))
                        {
                            xr.MoveToElement();
                            return true;
                        }
                        break;
                    }
                }
                while (xr.MoveToNextAttribute());
                xr.MoveToElement();
            }
            result = defaultValue;
            return false;
        }


        public static bool ValueAsBool(this XmlReader xr, bool defaultValue = false)
        {
            if (AsBool(xr.Value, out var r))
                return r;
            else
                return defaultValue;
        }
        public static T ValueAsNumber<T>(this XmlReader xr, T defaultValue = default) where T : INumber<T>
        {
            if (T.TryParse(xr.Value, CultureInfo.InvariantCulture, out var result))
                return result;
            else
                return defaultValue;
        }
        public static T? ValueAsNumber<T>(this XmlReader xr, T? defaultValue = default) where T : struct, INumber<T>
        {
            if (T.TryParse(xr.Value, CultureInfo.InvariantCulture, out var result))
                return result;
            else
                return defaultValue;
        }
        public static T ValueAsEnum<T>(this XmlReader xr, T defaultValue = default) where T : unmanaged, Enum
        {
            if (EnumHelper.TryAsEnum(xr.Value, out T result))
                return result;
            else
                return defaultValue;
        }

        private static bool AsBool(ReadOnlySpan<char> value, out bool result)
        {
            value = value.Trim();
            if (value.Length != 0)
            {
                if (value.Length == 4) { result = value[0] == 't' || value[0] == 'T'; return true; }
                if (value.Length == 1) { result = value[0] == '1'; return true; }
            }
            result = false;
            return false;
        }

        public readonly ref struct XREnumerator
        {
            private readonly XmlReader _xr;
            private readonly string _targetLocal;
            private readonly int _targetDepth;
            internal XREnumerator(XmlReader xr, string targetLocalName = null, bool targetChildren = false)
            {
                var depth = xr.Depth;
                if (targetChildren) { depth++; xr.Read(); }

                _xr = xr;
                _targetLocal = targetLocalName;
                _targetDepth = depth;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly XREnumerator GetEnumerator() => this;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public bool MoveNext()
            {
                do
                {
                    var depth = _xr.Depth;
                    if (depth < _targetDepth) { break; }
                    if (depth > _targetDepth) { continue; }
                    if (_xr.NodeType == XmlNodeType.Element &&
                        (_targetLocal == null || _targetLocal == _xr.LocalName))
                        return true;
                }
                while (_xr.Read());
                return false;
            }
            public string Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _xr.LocalName;
            }
        }
    }
}