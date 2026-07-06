namespace JHLib.Util.Cache
{
    public static class THelper<T>
    {
        public static readonly bool IsDisposable = typeof(IDisposable).IsAssignableFrom(typeof(T));
    }
}