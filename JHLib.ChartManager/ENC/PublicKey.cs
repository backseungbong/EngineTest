using System.Numerics;

namespace JHLib.ChartManager.ENC
{
    public class PublicKey
    {
        public BigInteger P;
        public BigInteger Q;
        public BigInteger G;
        public BigInteger Y;



        public PublicKey(BigInteger P, BigInteger Q, BigInteger G, BigInteger Y)
        {
            this.P = P;
            this.Q = Q;
            this.G = G;
            this.Y = Y;
        }



        public bool Authenticate(BigInteger digest, (BigInteger R, BigInteger S) sign)
        {
            BigInteger W = GetModularInverse(sign.S, this.Q);

            BigInteger U1 = (digest * W) % this.Q;
            BigInteger U2 = (sign.R * W) % this.Q;

            BigInteger V = ((BigInteger.ModPow(this.G, U1, this.P) * BigInteger.ModPow(this.Y, U2, this.P)) % this.P) % this.Q;

            return (V == sign.R);
        }

        private BigInteger GetModularInverse(BigInteger operand, BigInteger modulo)
        {
            BigInteger R1 = operand;
            BigInteger R2 = modulo;
            BigInteger X1 = 1;
            BigInteger X2 = 0;

            while (R2 > 0)
            {
                BigInteger quotient = R1 / R2;
                BigInteger modular = R1 % R2;
                BigInteger X = X1 - (quotient * X2);

                R1 = R2;
                R2 = modular;
                X1 = X2;
                X2 = X;
            }

            if (R1 == 1)
            {
                return ((X1 % modulo) + modulo) % modulo; // X1 결과가 음수일 때, 양수 Modular Inverse로 변환
            }
            else
            {
                return 0;
            }
        }
    }
}