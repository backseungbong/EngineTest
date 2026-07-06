using JHLib.ChartManager.Configuration;
using JHLib.ChartManager.Report;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;

namespace JHLib.ChartManager.Catalogue
{
    public static class SaCatalogue
    {
        public static Dictionary<string, ENC.Certificate> catalogue { get; private set; } = new Dictionary<string, ENC.Certificate>();

        public static bool loaded { get; private set; } = false;

        public static string fileName = "sa.cat";



        public static void Read()
        {
            FileInfo catalogueFile = new FileInfo(Path.Combine(DirectoryConfiguration.catalogue, SaCatalogue.fileName));

            if (catalogueFile.Exists)
            {
                SaCatalogue.Read(catalogueFile.FullName);
            }
            else
            {
                SaCatalogue.loaded = false;

                SaCatalogue.catalogue.Clear();

                SaCatalogue.loaded = true;
            }
        }

        public static void Read(string filePath)
        {
            SaCatalogue.loaded = false;

            SaCatalogue.catalogue.Clear();

            using (StreamReader reader = new StreamReader(filePath))
            {
                SaCatalogue.Read(reader);
            }

            SaCatalogue.loaded = true;
        }

        public static void Read(Stream fileStream)
        {
            SaCatalogue.loaded = false;

            SaCatalogue.catalogue.Clear();

            using (StreamReader reader = new StreamReader(fileStream))
            {
                SaCatalogue.Read(reader);
            }

            SaCatalogue.loaded = true;
        }

        private static void Read(StreamReader reader)
        {
            string? readLine = null;

            while ((readLine = reader.ReadLine()) != null)
            {
                if (!string.IsNullOrEmpty(readLine))
                {
                    string[] dataSegment = readLine.Split('@');

                    if (dataSegment.Length > 9)
                    {
                        ENC.Certificate certificate = new ENC.Certificate(dataSegment[9]) {
                            status = dataSegment[2],
                            C = dataSegment[4],
                            CN = dataSegment[5],
                            expirationDate = dataSegment[6],
                            effectiveDate = dataSegment[7],
                            L = dataSegment[8],
                        };

                        if (int.TryParse(dataSegment[1], out int type)) { certificate.type = type; }
                        if (DateTime.TryParse(dataSegment[3], out DateTime installTime)) { certificate.installTime = installTime.ToUniversalTime(); }
                        if (dataSegment.Length > 10) { certificate.O = dataSegment[10]; }
                        if (dataSegment.Length > 11) { certificate.OU = dataSegment[11]; }
                        if (dataSegment.Length > 12) { certificate.S = dataSegment[12]; }

                        SaCatalogue.Expiration(certificate);

                        if ((dataSegment.Length > 16) &&
                            BigInteger.TryParse(dataSegment[13], NumberStyles.HexNumber, null, out BigInteger P) &&
                            BigInteger.TryParse(dataSegment[14], NumberStyles.HexNumber, null, out BigInteger Q) &&
                            BigInteger.TryParse(dataSegment[15], NumberStyles.HexNumber, null, out BigInteger G) &&
                            BigInteger.TryParse(dataSegment[16], NumberStyles.HexNumber, null, out BigInteger Y))
                        {
                            certificate.publicKey = new ENC.PublicKey(P, Q, G, Y);
                        }

                        SaCatalogue.catalogue.TryAdd(certificate.name, certificate);
                    }
                }
            }
        }

        public static ENC.PublicKey? ReadPublicKey(string filePath)
        {
            string[] publicKeySegment = File.ReadAllLines(filePath);

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

                readable = headerOfP.Contains("//") && headerOfP.Contains("BIG P");
                readable = headerOfQ.Contains("//") && headerOfQ.Contains("BIG Q");
                readable = headerOfG.Contains("//") && headerOfG.Contains("BIG G");
                readable = headerOfY.Contains("//") && headerOfY.Contains("BIG Y");

                readable = dataOfP.Length > 159;
                readable = dataOfQ.Length > 49;
                readable = dataOfG.Length > 159;
                readable = dataOfY.Length > 159;

                if (readable)
                {
                    if (BigInteger.TryParse($"0{dataOfP.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger P) &&
                        BigInteger.TryParse($"0{dataOfQ.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger Q) &&
                        BigInteger.TryParse($"0{dataOfG.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger G) &&
                        BigInteger.TryParse($"0{dataOfY.Replace(".", "").Replace(" ", "")}", NumberStyles.HexNumber, null, out BigInteger Y))
                    {
                        return new ENC.PublicKey(P, Q, G, Y);
                    }
                }
            }

            return null;
        }


        public static bool Add(string filePath, bool saving = true)
        {
            return SaCatalogue.Add(new FileInfo(filePath), saving);
        }

        public static bool Add(FileInfo file, bool saving = true)
        {
            if (file.Exists && (file.Extension.ToUpper() == ".CRT"))
            {
                try
                {
                    FileInfo keyFile = new FileInfo(Path.Combine(file.DirectoryName ?? "", $"{Path.GetFileNameWithoutExtension(file.Name)}.PUB"));

                    if (keyFile.Exists)
                    {
                        return SaCatalogue.Add(
                            file.Name,
                            File.ReadAllBytes(file.FullName),
                            SaCatalogue.ReadPublicKey(keyFile.FullName),
                            saving
                        );
                    }
                    else
                    {
                        return SaCatalogue.Add(
                            file.Name,
                            File.ReadAllBytes(file.FullName),
                            saving
                        );
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.Message);

                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private static unsafe bool Add(string crtName, byte[] crt, ENC.PublicKey? publicKey, bool saving = true)
        {
            if (publicKey == null)
            {
                SSE.InvokeStandardError(SSE.StandardError.ERROR_08, crtName);

                return false;
            }

            if (crt.Length > 3)
            {
                ENC.Certificate certificate = new ENC.Certificate(crtName) {
                    type = 0,
                    status = "Installed",
                    installTime = DateTime.UtcNow,
                    publicKey = publicKey,
                };
                
                int index = 0;

                while (index < crt.Length)
                {
                    uint tag;

                    fixed (byte* tagPointer = &crt[index])
                    {
                        tag = *(uint*)tagPointer;
                    }

                    switch (tag)
                    {
                        case 0x04550306:
                            {
                                index += 4;

                                int dataType = crt[index++];

                                if (crt[index++] == 0x13)
                                {
                                    int dataLength = crt[index++];

                                    StringBuilder dataBuilder = new StringBuilder();

                                    for (int i = 0; i < dataLength; i++)
                                    {
                                        dataBuilder.Append((char)crt[index++]);
                                    }

                                    switch (dataType)
                                    {
                                        case 3: { certificate.CN = dataBuilder.ToString(); } break;
                                        case 6: { certificate.C = dataBuilder.ToString(); } break;
                                        case 7: { certificate.L = dataBuilder.ToString(); } break;
                                        case 8: { certificate.S = dataBuilder.ToString(); } break;
                                        case 10: { certificate.O = dataBuilder.ToString(); } break;
                                        case 11: { certificate.OU = dataBuilder.ToString(); } break;
                                    }
                                }
                            }
                            break;
                        case 0x0D171E30:
                            {
                                index += 4;

                                StringBuilder effectiveDateBuilder = new StringBuilder();

                                while (index < crt.Length)
                                {
                                    char read = (char)crt[index++];

                                    if (read == 'Z')
                                    {
                                        certificate.effectiveDate = effectiveDateBuilder.ToString();
                                        break;
                                    }
                                    else
                                    {
                                        effectiveDateBuilder.Append(read);
                                    }
                                }

                                ushort dataType;

                                fixed (byte* dataTypePointer = &crt[index])
                                {
                                    dataType = *(ushort*)dataTypePointer;
                                }

                                if (dataType == 0x0D17)
                                {
                                    index += 2;

                                    StringBuilder expirationDateBuilder = new StringBuilder();

                                    while (index < crt.Length)
                                    {
                                        char read = (char)crt[index++];

                                        if (read == 'Z')
                                        {
                                            certificate.expirationDate = expirationDateBuilder.ToString();
                                            break;
                                        }
                                        else
                                        {
                                            expirationDateBuilder.Append(read);
                                        }
                                    }
                                }
                            }
                            break;
                        default:
                            {
                                index++;
                            }
                            break;
                    }
                }

                if (!(certificate.O?.Contains("International Hydrographic Organization (IHO)") == true))
                {
                    SSE.InvokeStandardError(SSE.StandardError.ERROR_26, certificate.name);
                }

                SaCatalogue.Expiration(certificate);

                if (certificate.type == 2)
                {
                    SSE.InvokeStandardError(SSE.StandardError.ERROR_22, certificate.name);
                }

                SaCatalogue.catalogue[certificate.name] = certificate;

                if (saving) { SaCatalogue.Save(); }

                return true;
            }
            else
            {
                //SSE.InvokeStandardError(SSE.StandardError.ERROR_26);

                return false;
            }
        }

        private static unsafe bool Add(string crtName, byte[] crt, bool saving = true)
        {
            if (crt.Length > 3)
            {
                ENC.Certificate certificate = new ENC.Certificate(crtName) {
                    type = 0,
                    status = "Installed",
                    installTime = DateTime.UtcNow,
                };

                int index = 0;

                while (index < crt.Length)
                {
                    uint tag;

                    fixed (byte* tagPointer = &crt[index])
                    {
                        tag = *(uint*)tagPointer;
                    }

                    switch (tag)
                    {
                        case 0x04550306:
                            {
                                index += 4;

                                int dataType = crt[index++];

                                if (crt[index++] == 0x13)
                                {
                                    int dataLength = crt[index++];

                                    StringBuilder dataBuilder = new StringBuilder();

                                    for (int i = 0; i < dataLength; i++)
                                    {
                                        dataBuilder.Append((char)crt[index++]);
                                    }

                                    switch (dataType)
                                    {
                                        case 3: { certificate.CN = dataBuilder.ToString(); } break;
                                        case 6: { certificate.C = dataBuilder.ToString(); } break;
                                        case 7: { certificate.L = dataBuilder.ToString(); } break;
                                        case 8: { certificate.S = dataBuilder.ToString(); } break;
                                        case 10: { certificate.O = dataBuilder.ToString(); } break;
                                        case 11: { certificate.OU = dataBuilder.ToString(); } break;
                                    }
                                }
                            }
                            break;
                        case 0x0D171E30:
                            {
                                index += 4;

                                StringBuilder effectiveDateBuilder = new StringBuilder();

                                while (index < crt.Length)
                                {
                                    char read = (char)crt[index++];

                                    if (read == 'Z')
                                    {
                                        certificate.effectiveDate = effectiveDateBuilder.ToString();
                                        break;
                                    }
                                    else
                                    {
                                        effectiveDateBuilder.Append(read);
                                    }
                                }

                                ushort dataType;

                                fixed (byte* dataTypePointer = &crt[index])
                                {
                                    dataType = *(ushort*)dataTypePointer;
                                }

                                if (dataType == 0x0D17)
                                {
                                    index += 2;

                                    StringBuilder expirationDateBuilder = new StringBuilder();

                                    while (index < crt.Length)
                                    {
                                        char read = (char)crt[index++];

                                        if (read == 'Z')
                                        {
                                            certificate.expirationDate = expirationDateBuilder.ToString();
                                            break;
                                        }
                                        else
                                        {
                                            expirationDateBuilder.Append(read);
                                        }
                                    }
                                }
                            }
                            break;
                        default:
                            {
                                index++;

                                uint dataType = tag & 0x0000FFFF;

                                switch (dataType)
                                {
                                    case 0x4002: { index++; } break;
                                    case 0x4102: { index += 2; } break;
                                }

                                switch (dataType)
                                {
                                    case 0x4002:
                                    case 0x4102:
                                        {
                                            StringBuilder signatureBuilder = new StringBuilder();

                                            int pLength = 0;

                                            while (index < crt.Length)
                                            {
                                                signatureBuilder.Append(crt[index++].ToString("X2"));

                                                if (++pLength > 63) { break; }
                                            }

                                            string pString = $"0{signatureBuilder.ToString()}";

                                            signatureBuilder.Clear();

                                            switch (crt[++index])
                                            {
                                                case 0x14: { index++; } break;
                                                case 0x15: { index += 2; } break;
                                                default:
                                                    {
                                                        pString = string.Empty;
                                                        index = crt.Length;
                                                    }
                                                    break;
                                            }

                                            int qLength = 0;

                                            while (index < crt.Length)
                                            {
                                                signatureBuilder.Append(crt[index++].ToString("X2"));

                                                if (++qLength > 19) { break; }
                                            }

                                            string qString = $"0{signatureBuilder.ToString()}";

                                            signatureBuilder.Clear();

                                            switch (crt[++index])
                                            {
                                                case 0x40: { index++; } break;
                                                case 0x41: { index += 2; } break;
                                                default:
                                                    {
                                                        qString = string.Empty;
                                                        index = crt.Length;
                                                    }
                                                    break;
                                            }

                                            int gLength = 0;

                                            while (index < crt.Length)
                                            {
                                                signatureBuilder.Append(crt[index++].ToString("X2"));

                                                if (++gLength > 63) { break; }
                                            }

                                            string gString = $"0{signatureBuilder.ToString()}";

                                            signatureBuilder.Clear();

                                            index += 4;

                                            switch (crt[index])
                                            {
                                                case 0x40: { index++; } break;
                                                case 0x41: { index += 2; } break;
                                                default:
                                                    {
                                                        gString = string.Empty;
                                                        index = crt.Length;
                                                    }
                                                    break;
                                            }

                                            int yLength = 0;

                                            while (index < crt.Length)
                                            {
                                                signatureBuilder.Append(crt[index++].ToString("X2"));

                                                if (++yLength > 63) { break; }
                                            }

                                            string yString = $"0{signatureBuilder.ToString()}";

                                            // key는 crt말고 pub 우선?

                                            if (!string.IsNullOrEmpty(pString) &&
                                                !string.IsNullOrEmpty(qString) &&
                                                !string.IsNullOrEmpty(gString) &&
                                                !string.IsNullOrEmpty(yString) &&
                                                BigInteger.TryParse(pString, NumberStyles.HexNumber, null, out BigInteger P) &&
                                                BigInteger.TryParse(qString, NumberStyles.HexNumber, null, out BigInteger Q) &&
                                                BigInteger.TryParse(gString, NumberStyles.HexNumber, null, out BigInteger G) &&
                                                BigInteger.TryParse(yString, NumberStyles.HexNumber, null, out BigInteger Y))
                                            {
                                                certificate.publicKey = new ENC.PublicKey(P, Q, G, Y);
                                            }
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }

                if (certificate.publicKey == null)
                {
                    SSE.InvokeStandardError(SSE.StandardError.ERROR_08, certificate.name);

                    return false;
                }
                else
                {
                    if (!(certificate.O?.Contains("International Hydrographic Organization (IHO)") == true))
                    {
                        SSE.InvokeStandardError(SSE.StandardError.ERROR_26, certificate.name);
                    }

                    SaCatalogue.Expiration(certificate);

                    if (certificate.type == 2)
                    {
                        SSE.InvokeStandardError(SSE.StandardError.ERROR_22, certificate.name);
                    }

                    SaCatalogue.catalogue[certificate.name] = certificate;

                    if (saving) { SaCatalogue.Save(); }

                    return true;
                }
            }
            else
            {
                //SSE.InvokeStandardError(SSE.StandardError.ERROR_26);

                return false;
            }
        }


        public static bool Delete(ENC.Certificate certificate, bool saving = true)
        {
            return SaCatalogue.Delete(certificate.name, saving);
        }
        
        public static bool Delete(string name, bool saving = true)
        {
            if (SaCatalogue.catalogue.ContainsKey(name))
            {
                SaCatalogue.catalogue.Remove(name);

                if (saving) { SaCatalogue.Save(); }

                return true;
            }
            else
            {
                return false;
            }
        }


        public static void Save()
        {
            FileInfo catalogueFile = new FileInfo(Path.Combine(DirectoryConfiguration.catalogue, SaCatalogue.fileName));

            if (catalogueFile.Directory?.Exists == false) { catalogueFile.Directory.Create(); }

            SaCatalogue.Save(catalogueFile.FullName);
        }

        public static void Save(string filePath)
        {
            SaCatalogue.loaded = false;

            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                foreach (KeyValuePair<string, ENC.Certificate> certificateRecord in SaCatalogue.catalogue)
                {
                    SaCatalogue.Write(writer, certificateRecord.Value);
                }
            }

            SaCatalogue.loaded = true;
        }

        public static void Save(Stream fileStream)
        {
            SaCatalogue.loaded = false;

            using (StreamWriter writer = new StreamWriter(fileStream))
            {
                foreach (KeyValuePair<string, ENC.Certificate> certificateRecord in SaCatalogue.catalogue)
                {
                    SaCatalogue.Write(writer, certificateRecord.Value);
                }
            }

            SaCatalogue.loaded = true;
        }

        private static void Write(StreamWriter writer, ENC.Certificate certificate)
        {
            writer.Write(certificate.name); writer.Write('@');
            writer.Write(certificate.type); writer.Write('@');
            writer.Write(certificate.status); writer.Write('@');
            writer.Write(certificate.installTime?.ToString("O")); writer.Write('@');
            writer.Write(certificate.C); writer.Write('@');
            writer.Write(certificate.CN); writer.Write('@');
            writer.Write(certificate.expirationDate); writer.Write('@');
            writer.Write(certificate.effectiveDate); writer.Write('@');
            writer.Write(certificate.L); writer.Write('@');
            writer.Write(certificate.name); writer.Write('@');
            writer.Write(certificate.O); writer.Write('@');
            writer.Write(certificate.OU); writer.Write('@');
            writer.Write(certificate.S); writer.Write('@');
            writer.Write(certificate.publicKey?.P.ToString("X")); writer.Write('@');
            writer.Write(certificate.publicKey?.Q.ToString("X")); writer.Write('@');
            writer.Write(certificate.publicKey?.G.ToString("X")); writer.Write('@');
            writer.WriteLine(certificate.publicKey?.Y.ToString("X"));
            writer.Flush();
        }


        private static bool Expiration(ENC.Certificate certificate)
        {
            if (!string.IsNullOrEmpty(certificate.expirationDate))
            {
                DateTime now = DateTime.UtcNow;
                DateTime reference = DateTime.TryParseExact(
                    certificate.expirationDate switch {
                        _ when (certificate.expirationDate.Length == 8) => certificate.expirationDate[2..8],
                        _ when (certificate.expirationDate.Length == 14) => certificate.expirationDate[2..8],
                        _ when (certificate.expirationDate.Length == 12) => certificate.expirationDate[..6],
                        _ => "",
                    },
                    "yyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out DateTime expirationDate
                ) ? expirationDate.ToUniversalTime() : now;
                
                if (now.Date > reference.Date)
                {
                    certificate.type = 2;
                    certificate.status = "Expired";

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                certificate.type = 2;
                certificate.status = "Expired";

                return true;
            }
        }
    }
}