using Legacy.ECM_Core.Enumeration;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Legacy.ECM_Core.Component
{
    public partial class ChartOrganizer
    {
        private ENC.CellSignature? Read_Signature(StreamReader signature_reader, string cell_name)
        {
            ENC.CellSignature? Extract = null;

            string? FirstR_Header = signature_reader.ReadLine()?.ToUpper();
            string? FirstR_Data = signature_reader.ReadLine();
            string? FirstS_Header = signature_reader.ReadLine()?.ToUpper();
            string? FirstS_Data = signature_reader.ReadLine();

            string? SecondR_Header = signature_reader.ReadLine()?.ToUpper();
            string? SecondR_Data = signature_reader.ReadLine();
            string? SecondS_Header = signature_reader.ReadLine()?.ToUpper();
            string? SecondS_Data = signature_reader.ReadLine();

            string Public_Key = signature_reader.ReadToEnd();
            string[] PublicKey_Segment = Public_Key.Split("\r\n");

            if (PublicKey_Segment.Length > 7)
            {
                string P_Header = PublicKey_Segment[0].ToUpper();
                string P_Data = PublicKey_Segment[1];

                string Q_Header = PublicKey_Segment[2].ToUpper();
                string Q_Data = PublicKey_Segment[3];

                string G_Header = PublicKey_Segment[4].ToUpper();
                string G_Data = PublicKey_Segment[5];

                string Y_Header = PublicKey_Segment[6].ToUpper();
                string Y_Data = PublicKey_Segment[7];


                bool Readable = true;

                Readable = (FirstR_Header?.Contains("//") ?? false) && (FirstR_Header?.Contains("R:") ?? false);
                Readable = (FirstS_Header?.Contains("//") ?? false) && (FirstS_Header?.Contains("S:") ?? false);
                Readable = (SecondR_Header?.Contains("//") ?? false) && (SecondR_Header?.Contains("R:") ?? false);
                Readable = (SecondS_Header?.Contains("//") ?? false) && (SecondS_Header?.Contains("S:") ?? false);
                Readable = P_Header.Contains("//") && P_Header.Contains("BIG P");
                Readable = Q_Header.Contains("//") && Q_Header.Contains("BIG Q");
                Readable = G_Header.Contains("//") && G_Header.Contains("BIG G");
                Readable = Y_Header.Contains("//") && Y_Header.Contains("BIG Y");

                Readable = FirstR_Data?.Length > 39;
                Readable = FirstS_Data?.Length > 39;
                Readable = SecondR_Data?.Length > 39;
                Readable = SecondS_Data?.Length > 39;
                Readable = P_Data.Length > 127;
                Readable = Q_Data.Length > 39;
                Readable = G_Data.Length > 127;
                Readable = Y_Data.Length > 127;

                if (Readable)
                {
                    ENC.CellSignature Signature = new ENC.CellSignature();
                    Signature.Sign = new (BigInteger R, BigInteger S)[2];
                    Signature.Public_Key = (0, 0, 0, 0);

                    if (BigInteger.TryParse($"0{FirstR_Data?.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger R1))
                    {
                        Signature.Sign[0].R = R1;
                    }

                    if (BigInteger.TryParse($"0{FirstS_Data?.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger S1))
                    {
                        Signature.Sign[0].S = S1;
                    }

                    if (BigInteger.TryParse($"0{SecondR_Data?.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger R2))
                    {
                        Signature.Sign[1].R = R2;
                    }

                    if (BigInteger.TryParse($"0{SecondS_Data?.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger S2))
                    {
                        Signature.Sign[1].S = S2;
                    }

                    if (BigInteger.TryParse($"0{P_Data.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger P))
                    {
                        Signature.Public_Key.P = P;
                    }

                    if (BigInteger.TryParse($"0{Q_Data.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger Q))
                    {
                        Signature.Public_Key.Q = Q;
                    }

                    if (BigInteger.TryParse($"0{G_Data.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger G))
                    {
                        Signature.Public_Key.G = G;
                    }

                    if (BigInteger.TryParse($"0{Y_Data.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger Y))
                    {
                        Signature.Public_Key.Y = Y;
                    }

                    SHA1 SHA_1 = SHA1.Create();
                    Signature.Digest = new BigInteger(SHA_1.ComputeHash(Encoding.Default.GetBytes(Public_Key)), isUnsigned: true, isBigEndian: true);

                    Extract = Signature;
                }
                else
                {
                    StandardError.Invoke_Message(SSE.ERROR_24, cell_name);
                }
            }
            else
            {
                StandardError.Invoke_Message(SSE.ERROR_24, cell_name);
            }

            return Extract;
        }

        private bool Authenticate_EncCertificate(SA.Certificate certificate, ENC.CellSignature signature)
        {
            return Authenticate_Signature(signature.Digest, signature.Sign[1], certificate.Public_Key);
        }

        private bool Authenticate_EncCell(FileInfo cell, ENC.CellSignature signature)
        {
            using (FileStream Cell_Stream = cell.OpenRead())
            {
                SHA1 SHA_1 = SHA1.Create();
                BigInteger Digest = new BigInteger(SHA_1.ComputeHash(Cell_Stream), isUnsigned: true, isBigEndian: true);

                return Authenticate_Signature(Digest, signature.Sign[0], signature.Public_Key);
            }
        }

        private bool Authenticate_Signature(BigInteger digest, (BigInteger R, BigInteger S) sign, (BigInteger P, BigInteger Q, BigInteger G, BigInteger Y) public_key)
        {
            BigInteger W = Get_ModularInverse(sign.S, public_key.Q);

            BigInteger U1 = (digest * W) % public_key.Q;
            BigInteger U2 = (sign.R * W) % public_key.Q;

            BigInteger V = ((BigInteger.ModPow(public_key.G, U1, public_key.P) * BigInteger.ModPow(public_key.Y, U2, public_key.P)) % public_key.P) % public_key.Q;

            return (V == sign.R);
        }

        private BigInteger Get_ModularInverse(BigInteger operand, BigInteger modulo)
        {
            BigInteger R1 = operand;
            BigInteger R2 = modulo;
            BigInteger X1 = 1;
            BigInteger X2 = 0;

            while (R2 > 0)
            {
                BigInteger Quotient = R1 / R2;
                BigInteger Modular = R1 % R2;
                BigInteger X = X1 - (Quotient * X2);

                R1 = R2;
                R2 = Modular;
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