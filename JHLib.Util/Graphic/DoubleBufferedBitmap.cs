using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Simd;
using JHLib.Util.ThreadSafe;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Graphic
{
    public unsafe class DoubleBufferedBitmap(int w, int h) : IDisposable
    {
        private const int ALIGN_PITCHSIZE = 128; // must be power of 2
        private const int ALIGN_PITCHMASK = ALIGN_PITCHSIZE - 1;

        public static readonly DoubleBufferedBitmap Empty = new(-1, -1);
        public readonly ref struct BackBuffer
        {
            private readonly BufferInfo _info;
            internal BackBuffer(BufferInfo info) => _info = info;
            public readonly nint Buffer0 => _info.Buffer0;
            public void Return(Transform transform) => _info.BackReturn(transform);
        }

        public readonly ref struct FrontBuffer
        {
            private readonly BufferInfo _info;
            internal FrontBuffer(BufferInfo info) => _info = info;
            public readonly bool NewBuffer => _info.NewBuffer;
            public void CopyTo(nint dest0, uint destPitch) => _info.CopyTo(dest0, destPitch);
            public void Return() => _info.FrontReturn();
        }

        internal class BufferInfo(DoubleBufferedBitmap owner, nint buffer0)
        {
            private readonly DoubleBufferedBitmap _owner = owner;
            private Transform _transform;
            private uint _version;
            private int _locker;

            internal BufferInfo NextBuffer;

            public readonly nint Buffer0 = buffer0;
            public bool NewBuffer => _version != _owner._frontVersion;
            public Transform Transform => _transform;
            public void Dispose() => Interlocker.Lock(ref _locker);
            public bool TryLock() => Interlocked.CompareExchange(ref _locker, 1, 0) == 0;

            public void BackReturn(Transform transform)
            {
                _transform = transform;
                _version = ++_owner._backVersion;
                Volatile.Write(ref _locker, 0);
                _owner._target = this;
            }
            public void FrontReturn()
            {
                _owner._frontVersion = _version;
                Volatile.Write(ref _locker, 0);
            }
            public void CopyTo(nint dest0, uint destPitch)
            {
                _owner.Copy(Buffer0, dest0, destPitch);
            }
        }

        private nint _alloc0;
        private BufferInfo _buffer1;
        private BufferInfo _buffer2;
        private volatile BufferInfo _target;

        private uint _backVersion;
        private uint _frontVersion;

        public readonly int Width = w;
        public readonly int Height = h;

        /// <summary>
        /// 첫 BackBuffer 호출시 내부 비트맵 메모리가 Alloc 되므로,
        /// Dispose는 BackBuffer를 호출하는 스레드에서 호출하는것을 권장
        /// </summary>
        public void Dispose()
        {
            var alloc0 = Interlocked.Exchange(ref _alloc0, 0);
            if (alloc0 != 0)
            {
                _buffer1.Dispose();
                _buffer2.Dispose();
                NativeMemory.AlignedFree((void*)alloc0);
            }
        }

        private BufferInfo Initialize()
        {
            var needBytes = Width * Height * 4 + ALIGN_PITCHMASK & ~ALIGN_PITCHMASK;
            var alloc0 = (nint)NativeMemory.AlignedAlloc((uint)needBytes * 2, ALIGN_PITCHSIZE);

            var buffer1 = new BufferInfo(this, alloc0);
            var buffer2 = new BufferInfo(this, alloc0 + needBytes);
            buffer1.NextBuffer = buffer2;
            buffer2.NextBuffer = buffer1;

            _alloc0 = alloc0;
            _buffer1 = buffer1;
            _buffer2 = buffer2;

            _backVersion = 0;
            _frontVersion = 0;
            return buffer1;
        }

        public bool SameSize(int width, int height) => Width == width && Height == height;
        public bool GetBackbuffer(out BackBuffer buffer)
        {
            var t = _target ?? Initialize();

            t = t.NextBuffer;
            if (t.TryLock()) { goto OK; }

            t = t.NextBuffer;
            if (t.TryLock()) { goto OK; }

            Unsafe.SkipInit(out buffer);
            return false;

        OK: buffer = new(t);
            return true;
        }

        public Transform GetFrontbuffer(out FrontBuffer buffer)
        {
            var t = _target;
            if (t == null) { goto EX; }
            if (t.TryLock()) { goto OK; }

            t = t.NextBuffer;
            if (t.TryLock()) { goto OK; }

        EX: Unsafe.SkipInit(out buffer);
            return null;

        OK: buffer = new(t);
            return t.Transform;
        }

        private void Copy(nint src0, nint dst0, uint dstPitch)
        {
            var height = Height;
            var pitch = Width * 4;
            if (pitch == dstPitch)
            {
                if (SIMD.AlignCheck(dst0))
                    LightGraphic.BitmapCopy((byte*)src0, (byte*)dst0, pitch * height);
                else
                    Unsafe.CopyBlock((void*)dst0, (void*)src0, (uint)(pitch * height));
            }
            else
            {
                LightGraphic.BitmapCopyRow((byte*)src0, (byte*)dst0, pitch, (int)dstPitch, height);
            }
        }
    }
}