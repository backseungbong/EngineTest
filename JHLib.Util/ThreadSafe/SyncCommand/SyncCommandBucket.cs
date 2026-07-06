using JHLib.Util.Helper;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.ThreadSafe.SyncCommand
{
    public sealed class SyncCommandBucket<TKey, TValue>
    {
        private readonly SyncCommandSlot<TKey, TValue>[] _bucket;
        private ulong _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref SyncCommandSlot<TKey, TValue> GetBucket0(out ulong mask)
        {
            var buk = _bucket;
            mask = (ulong)buk.Length - 1;
            return ref MemoryMarshal.GetArrayDataReference(buk);
        }
        internal ulong CommandIndex => Volatile.Read(ref _index);

        public event Action OnNewCommand;
        public SyncCommandBucket(int bucketSize = 64)
        {
            var len = ArrayHelper.Pow2ArrayLength(bucketSize, 4);
            _bucket = new SyncCommandSlot<TKey, TValue>[len];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RaiseNewCommandEvent() => OnNewCommand?.Invoke();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Enqueue(SyncCommandType type)
        {
            ref var slot = ref NextSlot(out var seq);
            slot.Keys = default;
            slot.Value = default;
            slot.Type = type;
            Volatile.Write(ref slot.Sequence, seq + 1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Enqueue(SyncCommandType type, TValue value)
        {
            ref var slot = ref NextSlot(out var seq);
            slot.Keys = default;
            slot.Value = value;
            slot.Type = type;
            Volatile.Write(ref slot.Sequence, seq + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(SyncCommandType type, ReadOnlySpan<TKey> keys) => Enqueue(type, keys.ToArray());

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Enqueue(SyncCommandType type, TKey[] keys)
        {
            ref var slot = ref NextSlot(out var seq);
            slot.Keys = keys;
            slot.Value = default;
            slot.Type = type;
            Volatile.Write(ref slot.Sequence, seq + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref SyncCommandSlot<TKey, TValue> NextSlot(out ulong seq)
        {
            seq = Interlocked.Increment(ref _index) - 1;

            var buk = _bucket;
            var idx = seq & ((ulong)buk.Length - 1);
            return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buk), (nuint)idx);
        }
    }
}