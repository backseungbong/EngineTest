using System.Numerics;

namespace Legacy.ECM_Core.ENC
{
    public struct CellSignature
    {
        public (BigInteger R, BigInteger S)[] Sign;
        public (BigInteger P, BigInteger Q, BigInteger G, BigInteger Y) Public_Key;

        public BigInteger Digest;
    }
}