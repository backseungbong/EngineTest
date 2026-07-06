namespace JHLib.Util.ThreadSafe.SyncCommand
{
    public delegate void SyncCommandActionHandler<TKey, TValue>(
        SyncCommandType type, TValue value, TKey[] keys);

    internal struct SyncCommandSlot<TKey, TValue>
    {
        public ulong Sequence;
        public TKey[] Keys;
        public TValue Value;
        public SyncCommandType Type;
    }
}