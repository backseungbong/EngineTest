using System.Runtime.InteropServices;

namespace JHLib.Util.Helper
{
    public static class IndexHelper
    {
        /// <summary> 리스트 아이템을 예외처리 없이 가져온다 (유효한 인덱스는 true 및 아이템 반환, 아닐 시 false) </summary>
        public static bool TryGet<T>(List<T> list, int index, out T item) => TryGet(CollectionsMarshal.AsSpan(list), index, out item);

        /// <summary> 리스트 아이템을 예외처리 없이 가져온다 (유효한 인덱스는 true 및 아이템 반환, 아닐 시 false) </summary>
        public static bool TryGet<T>(T[] list, int index, out T item) => TryGet(list.AsSpan(), index, out item);

        /// <summary> 리스트 아이템을 예외처리 없이 가져온다 (유효한 인덱스는 true 및 아이템 반환, 아닐 시 false) </summary>
        public static bool TryGet<T>(Span<T> span, int index, out T item)
        {
            if ((uint)index < (uint)span.Length)
            {
                item = span[index];
                return true;
            }
            item = default;
            return false;
        }

        /// <summary> 리스트 아이템을 예외처리 없이 가져온다 (유효한 인덱스는 true 및 아이템 반환, 아닐 시 false) </summary>
        public static bool TryGet<T>(ArraySegment<T> list, int index, out T item)
        {
            if ((uint)index < (uint)list.Count)
            {
                item = list.Array[list.Offset + index];
                return true;
            }
            item = default;
            return false;
        }

        /// <summary> 리스트 아이템을 예외처리 없이 가져온다 (유효한 인덱스는 true 및 아이템 반환, 아닐 시 false) </summary>
        public static bool TryGet<T>(IList<T> list, int index, out T item)
        {
            if (list != null && (uint)index < (uint)list.Count)
            {
                try
                {
                    item = list[index]; // IList는 Count 체크이후라도 비동기적으로 외부에서 처리될경우 예외가 발생할수 있으므로 try-catch 처리
                    return true;
                }
                catch (Exception) { }
            }
            item = default;
            return false;
        }

        /// <summary> 리스트 아이템을 예외처리 없이 가져온다 (유효한 인덱스는 true 및 아이템 반환, 아닐 시 false) </summary>
        public static bool TryGet<T>(IReadOnlyList<T> list, int index, out T item)
        {
            if (list != null && (uint)index < (uint)list.Count)
            {
                item = list[index]; // IReadOnlyList는 고정된 리스트 이므로 Count 체크이후 예외가 발생하지 않는다고 가정
                return true;
            }
            item = default;
            return false;
        }
    }
}