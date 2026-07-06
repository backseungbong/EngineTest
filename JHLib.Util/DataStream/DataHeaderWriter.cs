using JHLib.Util.List;
using JHLib.Util.Pool;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.DataStream
{
    public readonly ref struct DataHeaderWriter
    {
        private readonly PoolStream _own;
        private readonly int _pos;
        private readonly int _dat;

        public readonly int HeaderPosition => _pos;
        public readonly int DataLength { get => _own.Position - _dat; set => _own.Position = _dat + value; }
        public readonly ref DataHeader Header => ref _own.Ref<DataHeader>(_pos);
        public readonly ref T Ref<T>(int position) => ref _own.Ref<T>(_dat + position);

        internal DataHeaderWriter(PoolStream own, int pos) { _own = own; _pos = pos; _dat = pos + DataHeader.SIZE; }

        public ref T EnsureSpace0<T>(int ensureCount) => ref _own.EnsureSpace0<T>(ensureCount, _dat);
        public OccupyWriter OccupyWriter(int byteSize) => _own.OccupyWriter(byteSize);
        public OccupyWriter OccupyWriter<T>(int count) => _own.OccupyWriter<T>(count);
        public ref byte Occupy0(int byteSize) => ref _own.Occupy0(byteSize);
        public ref T Occupy0<T>(int count) where T : unmanaged => ref _own.Occupy0<T>(count);

        public DataHeaderWriter AddHeader(int dataCode = 0, int itemCount = 0) => _own.AddHeader(dataCode, itemCount);
        public ref T AddRef<T>() where T : unmanaged => ref _own.AddRef<T>();
        public void Add<T>(T item) where T : unmanaged => _own.Add(item);
        public void Add(string str) => _own.Add(str);
        public void Add<T>(T[] array) where T : unmanaged => _own.Add(array, array.Length);
        public void Add<T>(SList<T> list) where T : unmanaged => _own.Add(ref list.Ref0, list.Count);
        public void Add<T>(DataRange<T> range) where T : unmanaged => _own.Add(range);
        public void Add<T>(DataHeaderReader<T> reader) where T : unmanaged => _own.Add(ref reader.Data0, reader.Count);


        /// <summary> 현재 Header 데이타를 Float2D Array로 가정하고 연속된 동일 좌표들을 제거 및 ItemCount를 업데이트 하고 ItemCount를 반환한다 </summary>
        public int F2Dedupe() => Header.ItemCount = _own.F2Dedupe(_dat);

        /// <summary> 현재 Header 데이타를 Float2D Array로 가정하고 연속된 동일 방향 좌표들을 제거 및 ItemCount를 업데이트 하고 ItemCount를 반환한다 </summary>
        public int F2DedupeV() => Header.ItemCount = _own.F2DedupeV(_dat);

        /// <summary> 현재 Header를 DataLength를 T 타입 사이즈로 카운트하여 ItemCount를 업데이트 하고 ItemCount를 반환한다 </summary>        
        public int UpdateCount<T>() => Header.ItemCount = DataLength / Unsafe.SizeOf<T>();

        /// <summary> 현재 Header의 ItemCount와 DataLength를 업데이트한다 </summary>        
        public void UpdateCount<T>(int count) { Header.ItemCount = count; DataLength = count * Unsafe.SizeOf<T>(); }

        /// <summary> T Reader로 변환한다 </summary>      
        public DataHeaderReader<T> AsReader<T>() where T : unmanaged => new(ref _own.Stream0, _pos);
    }
}