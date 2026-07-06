using System.Numerics;

namespace Legacy.ECM_Core.SA
{
    public struct Certificate
    {
        public int Type;
        public string Status;
        public string DateTime;

        public string Name;
        public string CN;
        public string OU;
        public string L;
        public string O;
        public string C;
        public string S;

        public string Effective_Date;
        public string Expiration_Date;

        public (BigInteger P, BigInteger Q, BigInteger G, BigInteger Y) Public_Key;
    }
}