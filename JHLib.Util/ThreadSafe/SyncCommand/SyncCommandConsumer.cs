using System.Runtime.CompilerServices;

namespace JHLib.Util.ThreadSafe.SyncCommand
{
    public sealed class SyncCommandConsumer<TKey, TValue>(SyncCommandBucket<TKey, TValue> owner)
    {
        private readonly SyncCommandBucket<TKey, TValue> _owner = owner;
        private ulong _index;
        private Lock _locker;

        public Action<TValue> InsertHandler { get; set; }
        public Action<TValue> UpdateHandler { get; set; }
        public Action<TKey[]> DeleteHandler { get; set; }
        public Action ReloadHandler { get; set; }
        public Action RefreshHandler { get; set; }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Sync() => _index != _owner.CommandIndex && Consume();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SyncWithLock() => _index != _owner.CommandIndex && ConsumeWithLock();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool ConsumeWithLock()
        {
            var locker = _locker;
            if (locker == null)
            {
                var newLock = new Lock();
                locker = Interlocked.CompareExchange(ref _locker, newLock, null) ?? newLock;
            }

            lock (locker)
                return Consume();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool Consume()
        {
            var idx = _index;
            var own = _owner;
            ref var buk0 = ref own.GetBucket0(out var msk);
            ref var slot = ref Unsafe.Add(ref buk0, (nuint)(idx & msk));
            if (idx + 1 == Volatile.Read(ref slot.Sequence)) { goto J1; }
            if (own.CommandIndex - idx > msk) { goto J2; }
            return false;

        J1: while (true)
            {
                var keys = slot.Keys;
                var value = slot.Value;
                var type = slot.Type;
                Volatile.ReadBarrier();

                if (own.CommandIndex - idx > msk) { goto J2; }
                if (type == SyncCommandType.Insert) InsertHandler?.Invoke(value);
                else if (type == SyncCommandType.Update) UpdateHandler?.Invoke(value);
                else if (type == SyncCommandType.Delete) DeleteHandler?.Invoke(keys);
                else if (type == SyncCommandType.Reload) { goto J2; }
                else RefreshHandler?.Invoke();

                slot = ref Unsafe.Add(ref buk0, (nuint)(++idx & msk));
                if (idx + 1 != Volatile.Read(ref slot.Sequence)) { goto J3; }
            }

        J2: idx = own.CommandIndex;
            ReloadHandler?.Invoke();
        J3: _index = idx;
            return true;
        }
    }
}