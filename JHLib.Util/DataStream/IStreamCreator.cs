using JHLib.Util.Pool;

namespace JHLib.Util.DataStream
{
    public interface IStreamCreator : IDisposable
    {
        int StreamSize { get; }
        int StreamCode { get; }
        int Count { get; }
        void CopyTo(PoolStream dest);
    }
}