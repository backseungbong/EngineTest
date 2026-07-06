namespace JHLib.Util.ThreadSafe.SyncCommand
{
    public enum SyncCommandType
    {
        Insert,
        Update,
        Delete,

        Reload = 100,
        Refresh,
    }
}