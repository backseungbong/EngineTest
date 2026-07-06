using System;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Serial
{
    internal class SentenceList
    {
        private string[] _buk = new string[32];
        private int _cap = 32;
        private int _cnt = 0;
        public string[] Bucket => _buk;
        public int Count => _cnt;
        public string this[int i] => _buk[i];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(string item)
        {
            var c = _cnt; _cnt = c + 1;
            if (c == _cap) Resize();
            _buk[c] = item;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize()
        {
            var c = _cap;
            var cap = c * 2;
            var buk = new string[cap];
            Array.Copy(_buk, 0, buk, 0, c);
            _buk = buk;
            _cap = cap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _cnt = 0;
    }
}