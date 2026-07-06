using JHLib.ChartManager.Report;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace JHLib.ChartManager.ENC
{
    public class Signature
    {
        public string cellName;

        public (BigInteger R, BigInteger S)[]? sign = null;
        public PublicKey? publicKey = null;

        public BigInteger? digest = null;



        public Signature(string cellName)
        {
            this.cellName = cellName;
        }



        public void Read(StreamReader reader)
        {
            string? firstHeaderOfR = reader.ReadLine()?.ToUpper();
            string? firstDataOfR = reader.ReadLine();
            string? firstHeaderOfS = reader.ReadLine()?.ToUpper();
            string? firstDataOfS = reader.ReadLine();

            string? secondHeaderOfR = reader.ReadLine()?.ToUpper();
            string? secondDataOfR = reader.ReadLine();
            string? secondHeaderOfS = reader.ReadLine()?.ToUpper();
            string? secondDataOfS = reader.ReadLine();

            string publicKey = reader.ReadToEnd();
            string[] publicKeySegment = publicKey.Split("\r\n");

            if (publicKeySegment.Length > 7)
            {
                string headerOfP = publicKeySegment[0].ToUpper();
                string dataOfP = publicKeySegment[1];

                string headerOfQ = publicKeySegment[2].ToUpper();
                string dataOfQ = publicKeySegment[3];

                string headerOfG = publicKeySegment[4].ToUpper();
                string dataOfG = publicKeySegment[5];

                string headerOfY = publicKeySegment[6].ToUpper();
                string dataOfY = publicKeySegment[7];

                bool readable = true;

                readable = (firstHeaderOfR?.Contains("//") ?? false) && (firstHeaderOfR?.Contains("R:") ?? false);
                readable = (firstHeaderOfS?.Contains("//") ?? false) && (firstHeaderOfS?.Contains("S:") ?? false);
                readable = (secondHeaderOfR?.Contains("//") ?? false) && (secondHeaderOfR?.Contains("R:") ?? false);
                readable = (secondHeaderOfS?.Contains("//") ?? false) && (secondHeaderOfS?.Contains("S:") ?? false);
                readable = headerOfP.Contains("//") && headerOfP.Contains("BIG P");
                readable = headerOfQ.Contains("//") && headerOfQ.Contains("BIG Q");
                readable = headerOfG.Contains("//") && headerOfG.Contains("BIG G");
                readable = headerOfY.Contains("//") && headerOfY.Contains("BIG Y");

                readable = firstDataOfR?.Length > 49;
                readable = firstDataOfS?.Length > 49;
                readable = secondDataOfR?.Length > 49;
                readable = secondDataOfS?.Length > 49;
                readable = dataOfP.Length > 159;
                readable = dataOfQ.Length > 49;
                readable = dataOfG.Length > 159;
                readable = dataOfY.Length > 159;

                if (readable)
                {
                    this.sign = new (BigInteger R, BigInteger S)[2];

                    if (BigInteger.TryParse($"0{firstDataOfR?.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger R1))
                    {
                        this.sign[0].R = R1;
                    }

                    if (BigInteger.TryParse($"0{firstDataOfS?.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger S1))
                    {
                        this.sign[0].S = S1;
                    }

                    if (BigInteger.TryParse($"0{secondDataOfR?.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger R2))
                    {
                        this.sign[1].R = R2;
                    }

                    if (BigInteger.TryParse($"0{secondDataOfS?.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger S2))
                    {
                        this.sign[1].S = S2;
                    }

                    if (BigInteger.TryParse($"0{dataOfP.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger P) &&
                        BigInteger.TryParse($"0{dataOfQ.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger Q) &&
                        BigInteger.TryParse($"0{dataOfG.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger G) &&
                        BigInteger.TryParse($"0{dataOfY.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger Y))
                    {
                        this.publicKey = new PublicKey(P, Q, G, Y);
                    }

                    SHA1 SHA_1 = SHA1.Create();
                    this.digest = new BigInteger(SHA_1.ComputeHash(Encoding.Default.GetBytes(publicKey)), isUnsigned: true, isBigEndian: true);
                }
                else
                {
                    SSE.InvokeStandardError(SSE.StandardError.ERROR_24, this.cellName);
                }
            }
            else
            {
                SSE.InvokeStandardError(SSE.StandardError.ERROR_24, this.cellName);
            }
        }

        public bool Authenticate(FileInfo cellFile)
        {
            if ((this.sign != null) && (this.publicKey != null))
            {
                using (FileStream cellStream = cellFile.OpenRead())
                {
                    SHA1 SHA_1 = SHA1.Create();
                    BigInteger cellDigest = new BigInteger(SHA_1.ComputeHash(cellStream), isUnsigned: true, isBigEndian: true);

                    return this.publicKey.Authenticate(cellDigest, this.sign[0]);
                }
            }
            else
            {
                return false;
            }
        }
    }
}