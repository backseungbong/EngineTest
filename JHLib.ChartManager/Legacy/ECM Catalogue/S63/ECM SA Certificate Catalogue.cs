using Legacy.ECM_Core.Enumeration;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;

namespace Legacy.ECM_Core.Catalogue
{
    public static class SaCatalogue
    {
        public static Dictionary<string, SA.Certificate> Catalogue { get; private set; } = new Dictionary<string, SA.Certificate>();



        public static void Read(string file_path)
        {
            Catalogue.Clear();

            using (StreamReader Catalogue_Reader = new StreamReader(file_path))
            {
                SaCatalogue.Read(Catalogue_Reader);
            }
        }

        public static void Read(Stream file_stream)
        {
            Catalogue.Clear();

            using (StreamReader Catalogue_Reader = new StreamReader(file_stream))
            {
                SaCatalogue.Read(Catalogue_Reader);
            }
        }

        private static void Read(StreamReader catalogue_reader)
        {
            while (true)
            {
                string? Data_Sentence = catalogue_reader.ReadLine();

                if (Data_Sentence != null)
                {
                    if (!string.IsNullOrEmpty(Data_Sentence))
                    {
                        SA.Certificate Certificate = new SA.Certificate();
                        string[] Data_Segment = Data_Sentence.Split('@');

                        if (Data_Segment.Length > 1) { Certificate.Type = int.TryParse(Data_Segment[1], out int Type) ? Type : -1; }
                        if (Data_Segment.Length > 2) { Certificate.Status = Data_Segment[2]; }
                        if (Data_Segment.Length > 3) { Certificate.DateTime = Data_Segment[3]; }

                        if (Data_Segment.Length > 4) { Certificate.C = Data_Segment[4]; }
                        if (Data_Segment.Length > 5) { Certificate.CN = Data_Segment[5]; }

                        if (Data_Segment.Length > 6) { Certificate.Expiration_Date = Data_Segment[6]; }
                        if (Data_Segment.Length > 7) { Certificate.Effective_Date = Data_Segment[7]; }

                        if (Data_Segment.Length > 8) { Certificate.L = Data_Segment[8]; }
                        if (Data_Segment.Length > 9) { Certificate.Name = Data_Segment[9]; }
                        if (Data_Segment.Length > 10) { Certificate.O = Data_Segment[10]; }
                        if (Data_Segment.Length > 11) { Certificate.OU = Data_Segment[11]; }
                        if (Data_Segment.Length > 12) { Certificate.S = Data_Segment[12]; }

                        if (Expiration(Certificate))
                        {
                            Certificate.Type = 2;
                            Certificate.Status = "Expired";
                        }

                        Certificate.Public_Key = (0, 0, 0, 0);

                        if (Data_Segment.Length > 13) {
                            if (BigInteger.TryParse($"0{Data_Segment[13]}", NumberStyles.HexNumber, null, out BigInteger P))
                            {
                                Certificate.Public_Key.P = P;
                            }
                        }

                        if (Data_Segment.Length > 14)
                        {
                            if (BigInteger.TryParse($"0{Data_Segment[14]}", NumberStyles.HexNumber, null, out BigInteger Q))
                            {
                                Certificate.Public_Key.Q = Q;
                            }
                        }

                        if (Data_Segment.Length > 15)
                        {
                            if (BigInteger.TryParse($"0{Data_Segment[15]}", NumberStyles.HexNumber, null, out BigInteger G))
                            {
                                Certificate.Public_Key.G = G;
                            }
                        }

                        if (Data_Segment.Length > 16)
                        {
                            if (BigInteger.TryParse($"0{Data_Segment[16]}", NumberStyles.HexNumber, null, out BigInteger Y))
                            {
                                Certificate.Public_Key.Y = Y;
                            }
                        }

                        if (!string.IsNullOrEmpty(Certificate.Name))
                        {
                            Catalogue.TryAdd(Certificate.Name, Certificate);
                        }
                    }
                }
                else
                {
                    break;
                }
            }
        }


        public static void Add_SA(string file_path)
        {
            if (File.Exists(file_path) && (Path.GetExtension(file_path).ToUpper() == ".CRT"))
            {
                byte[] CRT_Data = File.ReadAllBytes(file_path);

                SaCatalogue.Add_SA(Path.GetFileName(file_path), CRT_Data);
            }
            else
            {

            }
        }

        public static void Add_SA(FileInfo file_info)
        {
            if (file_info.Exists && (file_info.Extension.ToUpper() == ".CRT"))
            {
                byte[] CRT_Data = File.ReadAllBytes(file_info.FullName);

                SaCatalogue.Add_SA(file_info.Name, CRT_Data);
            }
            else
            {

            }
        }

        private static unsafe void Add_SA(string crt_name, byte[] crt_data)
        {
            if (crt_data.Length > 3)
            {
                SA.Certificate Certificate = new SA.Certificate();
                Certificate.Name = crt_name;
                Certificate.Type = 0;
                Certificate.Status = "Installed";
                Certificate.DateTime = DateTime.Now.ToString("yyyy-MM-ddHH:mm:ss");

                int Index = 0;
                
                while (Index < crt_data.Length)
                {
                    uint Tag;

                    fixed (byte* Tag_Pointer = &crt_data[Index])
                    {
                        Tag = *(uint*)Tag_Pointer;
                    }

                    switch (Tag)
                    {
                        case 0x04550306:
                            {
                                Index += 4;

                                int Data_Type = crt_data[Index++];

                                if (crt_data[Index++] == 0x13)
                                {
                                    StringBuilder Sentence_Builder = new StringBuilder();
                                    int Sentence_Length = crt_data[Index++]; // 뭔가 이상하면 이 부분부터 다시 볼 것

                                    for (int i = 0; i < Sentence_Length; i++)
                                    {
                                        Sentence_Builder.Append((char)crt_data[Index++]);
                                    }

                                    switch (Data_Type)
                                    {
                                        case 3: { Certificate.CN = Sentence_Builder.ToString(); } break;
                                        case 6: { Certificate.C = Sentence_Builder.ToString(); } break;
                                        case 7: { Certificate.L = Sentence_Builder.ToString(); } break;
                                        case 8: { Certificate.S = Sentence_Builder.ToString(); } break;
                                        case 10: { Certificate.O = Sentence_Builder.ToString(); } break;
                                        case 11: { Certificate.OU = Sentence_Builder.ToString(); } break;
                                    }
                                }
                            }
                            break;
                        case 0x0D171E30:
                            {
                                Index += 4;

                                StringBuilder EffectiveDate_Builder = new StringBuilder();

                                while (Index < crt_data.Length)
                                {
                                    char Data = (char)crt_data[Index++];

                                    if (Data == 'Z')
                                    {
                                        Certificate.Effective_Date = EffectiveDate_Builder.ToString();
                                        break;
                                    }
                                    else
                                    {
                                        EffectiveDate_Builder.Append(Data);
                                    }
                                }


                                ushort Sentence_Type;

                                fixed (byte* Sentence_Pointer = &crt_data[Index])
                                {
                                    Sentence_Type = *(ushort*)Sentence_Pointer;
                                }

                                if (Sentence_Type == 0x0D17)
                                {
                                    Index += 2;

                                    StringBuilder ExpirationDate_Builder = new StringBuilder();

                                    while (Index < crt_data.Length)
                                    {
                                        char Data = (char)crt_data[Index++];

                                        if (Data == 'Z')
                                        {
                                            Certificate.Expiration_Date = ExpirationDate_Builder.ToString();
                                            break;
                                        }
                                        else
                                        {
                                            ExpirationDate_Builder.Append(Data);
                                        }
                                    }
                                }
                            }
                            break;
                        default:
                            {
                                Index++;

                                uint Sentence_Type = Tag & 0x0000FFFF;

                                if ((Sentence_Type == 0x4002) || (Sentence_Type == 0x4102))
                                {
                                    StringBuilder Signature_Builder = new StringBuilder();


                                    switch (Sentence_Type)
                                    {
                                        case 0x4002: { Index++; } break;
                                        case 0x4102: { Index += 2; } break;
                                    }

                                    int P_Length = 0;

                                    while (Index < crt_data.Length)
                                    {
                                        Signature_Builder.Append(crt_data[Index++].ToString("X2"));

                                        if (++P_Length > 63) { break; }
                                    }

                                    if (BigInteger.TryParse($"0{Signature_Builder.ToString()}", NumberStyles.HexNumber, null, out BigInteger P))
                                    {
                                        Certificate.Public_Key.P = P;
                                    }

                                    Signature_Builder.Clear();


                                    Index++;

                                    switch (crt_data[Index])
                                    {
                                        case 0x14: { Index++; } break;
                                        case 0x15: { Index += 2; } break;
                                        default: { StandardError.Invoke_Message(SSE.ERROR_26); } return;
                                    }

                                    int Q_Length = 0;

                                    while (Index < crt_data.Length)
                                    {
                                        Signature_Builder.Append(crt_data[Index++].ToString("X2"));

                                        if (++Q_Length > 19) { break; }
                                    }

                                    if (BigInteger.TryParse($"0{Signature_Builder.ToString()}", NumberStyles.HexNumber, null, out BigInteger Q))
                                    {
                                        Certificate.Public_Key.Q = Q;
                                    }

                                    Signature_Builder.Clear();


                                    Index++;

                                    switch (crt_data[Index])
                                    {
                                        case 0x40: { Index++; } break;
                                        case 0x41: { Index += 2; } break;
                                        default: { StandardError.Invoke_Message(SSE.ERROR_26); } return;
                                    }

                                    int G_Length = 0;

                                    while (Index < crt_data.Length)
                                    {
                                        Signature_Builder.Append(crt_data[Index++].ToString("X2"));

                                        if (++G_Length > 63) { break; }
                                    }

                                    if (BigInteger.TryParse($"0{Signature_Builder.ToString()}", NumberStyles.HexNumber, null, out BigInteger G))
                                    {
                                        Certificate.Public_Key.G = G;
                                    }

                                    Signature_Builder.Clear();


                                    Index += 4;

                                    switch (crt_data[Index])
                                    {
                                        case 0x40: { Index++; } break;
                                        case 0x41: { Index += 2; } break;
                                        default: { StandardError.Invoke_Message(SSE.ERROR_26); } return;
                                    }

                                    int Y_Index = 0;

                                    while (Index < crt_data.Length)
                                    {
                                        Signature_Builder.Append(crt_data[Index++].ToString("X2"));

                                        if (++Y_Index > 63) { break; }
                                    }

                                    if (BigInteger.TryParse($"0{Signature_Builder.ToString()}", NumberStyles.HexNumber, null, out BigInteger Y))
                                    {
                                        Certificate.Public_Key.Y = Y;
                                    }

                                    Signature_Builder.Clear();
                                }
                            }
                            break;
                    }
                }

                
                if (!(Certificate.O?.Contains("International Hydrographic Organization (IHO)") == true))
                {
                    StandardError.Invoke_Message(SSE.ERROR_26);
                }

                //if (SaCatalogue.Expiration(Certificate)) // TDS test
                //{
                //    Certificate.Type = 2;
                //    Certificate.Status = "Expired";

                //    StandardError.Invoke_Message(SSE.ERROR_22);
                //}
                //else
                {
                    if (SaCatalogue.Catalogue.ContainsKey(Certificate.Name))
                    {
                        SaCatalogue.Catalogue[Certificate.Name] = Certificate;
                    }
                    else
                    {
                        SaCatalogue.Catalogue.Add(Certificate.Name, Certificate);
                    }

                    //SaCatalogue.Save_SA(???); //나중에 활성화 시킬 것
                }
            }
            else
            {
                StandardError.Invoke_Message(SSE.ERROR_26);
            }
        }


        public static void Save_SA(string file_path)
        {
            FileInfo SA_FileInfo = new FileInfo(file_path);

            if (SA_FileInfo.Directory?.Exists == false) { SA_FileInfo.Directory.Create(); }
            if (SA_FileInfo.Exists) { SA_FileInfo.Delete(); }

            using (StreamWriter Writer = new StreamWriter(SA_FileInfo.FullName))
            {
                foreach (KeyValuePair<string, SA.Certificate> Catalogue in SaCatalogue.Catalogue)
                {
                    Writer.Write(Catalogue.Key); Writer.Write('@');
                    Writer.Write(Catalogue.Value.Type); Writer.Write('@');
                    Writer.Write(Catalogue.Value.Status); Writer.Write('@');
                    Writer.Write(Catalogue.Value.DateTime); Writer.Write('@');
                    Writer.Write(Catalogue.Value.C); Writer.Write('@');
                    Writer.Write(Catalogue.Value.CN); Writer.Write('@');
                    Writer.Write(Catalogue.Value.Expiration_Date); Writer.Write('@');
                    Writer.Write(Catalogue.Value.Effective_Date); Writer.Write('@');
                    Writer.Write(Catalogue.Value.L); Writer.Write('@');
                    Writer.Write(Catalogue.Value.Name); Writer.Write('@');
                    Writer.Write(Catalogue.Value.O); Writer.Write('@');
                    Writer.Write(Catalogue.Value.OU); Writer.Write('@');
                    Writer.Write(Catalogue.Value.S); Writer.Write('@');
                    Writer.Write(Catalogue.Value.Public_Key.P); Writer.Write('@'); // Signature 사용 체계를 바꾸어서 이전과는 호환이 안 되는 파일 구조로 바꿈 (입출력 편의성 때문에)
                    Writer.Write(Catalogue.Value.Public_Key.Q); Writer.Write('@');
                    Writer.Write(Catalogue.Value.Public_Key.G); Writer.Write('@');
                    Writer.Write(Catalogue.Value.Public_Key.Y); Writer.WriteLine('@');

                    Writer.Flush();
                }
            }
        }


        private static bool Expiration(SA.Certificate certificate)
        {
            bool Result = true;

            if (!string.IsNullOrEmpty(certificate.Expiration_Date))
            {
                DateTime Now = DateTime.UtcNow;
                DateTime Reference = Now;

                if (certificate.Expiration_Date.Length == 8)
                {
                    Reference = DateTime.TryParseExact(certificate.Expiration_Date[2..8], "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime Expiration_Date) ? Expiration_Date.ToUniversalTime() : Now;
                }
                else if (certificate.Expiration_Date.Length == 14)
                {
                    Reference = DateTime.TryParseExact(certificate.Expiration_Date[2..8], "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime Expiration_Date) ? Expiration_Date.ToUniversalTime() : Now;
                }
                else if (certificate.Expiration_Date.Length == 12)
                {
                    Reference = DateTime.TryParseExact(certificate.Expiration_Date[..6], "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime Expiration_Date) ? Expiration_Date.ToUniversalTime() : Now;
                }

                Result = Now.Date > Reference.Date;
            }

            return Result;
        }
    }
}