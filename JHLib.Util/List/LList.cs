using JHLib.Util.ArrayControl;
using JHLib.Util.Helper;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.List
{
    using static JHLib.Util.Helper.RefCommand;

    /// <summary>
    /// 링크드 리스트 자료구조 <para/>
    /// 기존의 LinkedList보다 가볍고 빠르게 동작하도록 구현 <para/>
    /// class 참조주소를 통한 연결 방식을 인덱싱 방식으로 변경하여 메모리와 성능이 효율적으로 관리됨 <para/>
    /// 여러 케이스에서 2~5배까지 LinkedList보다 빠르며, 편의를 위한 추가 함수의 확장가능 
    /// </summary>
    public class LList<T>
    {
        private struct Node { public int Prev; public int Next; }

        private Node[] _nod;
        private T[] _val;
        private int _cap;
        private int _cnt;
        private int _fnt;
        private int _fdx;
        private int _head;

        public int Count => _cnt - _fnt;
        public T First => _val[_head];
        public T Last => _val[_nod[_head].Prev];
        public T GetValue(int nodeIndex) => _val[nodeIndex];


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LList() => Initialize(2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LList(int cap) => Initialize(MathHelper.RoundUpToPow2(2, cap));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Initialize(int cap)
        {
            _nod = new Node[cap];
            _val = new T[cap];
            _cap = cap;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize()
        {
            var cap = _cap;
            _nod = AC.CopyNew(_nod, cap * 2, cap);
            _val = RefCopyNew(_val, cap * 2, cap);
            _cap = cap * 2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int AddFirst(T val)
        {
            var i = _fnt;
            if (i == 0)
            {
                i = _cnt; _cnt = i + 1;
                if (_cap == i) Resize();
            }
            else
            {
                _fnt = i - 1; i = _fdx;
                _fdx = _nod[i].Next;
            }

            AddLink(ref RefT(_nod), _head, i); _head = i;

            _val[i] = val;
            return i;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int AddLast(T val)
        {
            var i = _fnt;
            if (i == 0)
            {
                i = _cnt; _cnt = i + 1;
                if (_cap == i) Resize();
            }
            else
            {
                _fnt = i - 1; i = _fdx;
                _fdx = _nod[i].Next;
            }

            AddLink(ref RefT(_nod), _head, i);

            _val[i] = val;
            return i;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveFirst() => PopFirst(out _);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool PopFirst(out T val)
        {
            if (_fnt < _cnt)
            {
                ref var n = ref RefT(_nod);
                var i = _head; _head = AddT(ref n, i).Next;
                RmvLink(ref n, i);
                AddT(ref n, i).Next = _fdx; _fdx = i; _fnt++;
                val = _val[i]; _val[i] = default;
                return true;
            }
            val = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveLast() => PopLast(out _);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool PopLast(out T val)
        {
            if (_fnt < _cnt)
            {
                ref var n = ref RefT(_nod);
                var i = AddT(ref n, _head).Prev;
                RmvLink(ref n, i);
                AddT(ref n, i).Next = _fdx; _fdx = i; _fnt++;
                val = _val[i]; _val[i] = default;
                return true;
            }
            val = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int Insert(T val, int nodeIndex)
        {
            var i = _fnt;
            if (i == 0)
            {
                i = _cnt; _cnt = i + 1;
                if (_cap == i) Resize();
            }
            else
            {
                _fnt = i - 1; i = _fdx;
                _fdx = _nod[i].Next;
            }

            AddLink(ref RefT(_nod), nodeIndex, i);

            if (_head == nodeIndex)
                _head = i;

            _val[i] = val;
            return i;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Remove(int nodeIndex)
        {
            ref var n = ref RefT(_nod);
            if (_head == nodeIndex) _head = AddT(ref n, nodeIndex).Next;
            RmvLink(ref n, nodeIndex);
            AddT(ref n, nodeIndex).Next = _fdx; _fdx = nodeIndex; _fnt++;
            _val[nodeIndex] = default;
        }

        /// <summary> 첫번째 값을 마지막 위치로 이동시킨다 </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FirstToLast() => _head = _nod[_head].Next;

        /// <summary> 마지막 값을 첫번째 위치로 이동시킨다 </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LastToFirst() => _head = _nod[_head].Prev;


        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Clear()
        {
            _nod[0] = default;
            _cnt = 0;
            _fnt = 0;
            _fdx = 0;
            _head = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddLink(ref Node n, int next, int add)
        {
            var prev = Unsafe.Add(ref n, next).Prev;
            Unsafe.Add(ref n, prev).Next = add;
            Unsafe.Add(ref n, next).Prev = add;
            Unsafe.Add(ref n, add).Prev = prev;
            Unsafe.Add(ref n, add).Next = next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RmvLink(ref Node n, int rmv)
        {
            var prev = Unsafe.Add(ref n, rmv).Prev;
            var next = Unsafe.Add(ref n, rmv).Next;
            Unsafe.Add(ref n, next).Prev = prev;
            Unsafe.Add(ref n, prev).Next = next;
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref struct NodeInfo(ref T v, int i)
        {
            private readonly ref T _val = ref v;
            public readonly ref T Value => ref Unsafe.Add(ref _val, i);
            public readonly int Index => i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new(this);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref struct Enumerator(LList<T> list)
        {
            private readonly ref Node _ent = ref MemoryMarshal.GetArrayDataReference(list._nod);
            private readonly ref T _val = ref MemoryMarshal.GetArrayDataReference(list._val);
            private int _cnt = list.Count;
            private int _nxt = list._head;
            private int _idx;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly Enumerator GetEnumerator() => this;
            public readonly NodeInfo Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(ref _val, _idx);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                _nxt = Unsafe.Add(ref _ent, _idx = _nxt).Next;
                return --_cnt >= 0;
            }
        }
    }
}