using JHLib.ChartManager.Configuration;
using JHLib.ChartManager.Report;
using System.Globalization;
using System.IO;

namespace JHLib.ChartManager.Catalogue
{
    public static class PermitCatalogue
    {
        public static Dictionary<string, List<ENC.Permit>> catalogue { get; private set; } = new Dictionary<string, List<ENC.Permit>>();

        public static bool loaded { get; private set; } = false;

        public static string fileName = "permit.cat";
        public static string HWID = "12345";



        public static void LoadUserPermit(string userPermit, string userKey)
        {
            if (userPermit.Length > 27)
            {
                string userEncryptKey = userPermit[0..16].ToLower();
                string userCRC = userPermit[16..24].ToUpper();

                Cipher.BlowFish blowFish = new Cipher.BlowFish(userKey);

                string userDecryptKey = blowFish.DecryptKey(userEncryptKey);

                if (!string.IsNullOrEmpty(userDecryptKey) && (userDecryptKey.Length > 8))
                {
                    PermitCatalogue.HWID = $"{userDecryptKey[1]}{userDecryptKey[3]}{userDecryptKey[5]}{userDecryptKey[7]}{userDecryptKey[9]}";

                    string encryptKey = blowFish.EncryptKey(PermitCatalogue.HWID);
                    bool result = Cipher.CRC.ValidateCRC(blowFish, encryptKey, userCRC, false);

                    if (!result)
                    {
                        int stop = 0;
                    }
                }
            }
        }
        
        public static void Read()
        {
            FileInfo catalogueFile = new FileInfo(Path.Combine(DirectoryConfiguration.catalogue, PermitCatalogue.fileName));

            if (catalogueFile.Exists)
            {
                PermitCatalogue.Read(catalogueFile.FullName);
            }
            else
            {
                PermitCatalogue.loaded = false;

                PermitCatalogue.catalogue.Clear();

                PermitCatalogue.loaded = true;
            }
        }

        public static void Read(string filePath)
        {
            PermitCatalogue.loaded = false;

            PermitCatalogue.catalogue.Clear();

            using (StreamReader reader = new StreamReader(filePath))
            {
                PermitCatalogue.Read(reader);
            }

            PermitCatalogue.loaded = true;
        }

        public static void Read(Stream fileStream)
        {
            PermitCatalogue.loaded = false;

            PermitCatalogue.catalogue.Clear();

            using (StreamReader reader = new StreamReader(fileStream))
            {
                PermitCatalogue.Read(reader);
            }

            PermitCatalogue.loaded = true;
        }

        private static void Read(StreamReader reader)
        {
            string? readLine = null;

            while ((readLine = reader.ReadLine()) != null)
            {
                if (!string.IsNullOrEmpty(readLine))
                {
                    string[] dataSegment = readLine.Split('@');
                    ENC.Permit permit = new ENC.Permit(dataSegment[0]);

                    if (!string.IsNullOrEmpty(permit.name))
                    {
                        if ((dataSegment.Length > 1) && int.TryParse(dataSegment[1], out int error)) { permit.error = (ENC.Permit.Validation)error; }
                        if ((dataSegment.Length > 2) && int.TryParse(dataSegment[2], out int type)) { permit.type = type; }
                        if (dataSegment.Length > 3) { permit.checksum = dataSegment[3]; }
                        if (dataSegment.Length > 4) { permit.expirationDate = dataSegment[4]; }
                        if (dataSegment.Length > 6) { permit.key = (X: dataSegment[5], Y: dataSegment[6]); }
                        if (dataSegment.Length > 7) { permit.comment = dataSegment[7]; }
                        if (dataSegment.Length > 8) { permit.DSID = dataSegment[8]; }
                        if ((dataSegment.Length > 9) && int.TryParse(dataSegment[9], out int EDTN)) { permit.EDTN = EDTN; }
                        if ((dataSegment.Length > 10) && int.TryParse(dataSegment[10], out int serviceLevel)) { permit.serviceLevel = serviceLevel; }

                        PermitCatalogue.Expiration(permit);

                        if (!PermitCatalogue.catalogue.ContainsKey(permit.name)) { PermitCatalogue.catalogue.Add(permit.name, new List<ENC.Permit>()); }

                        PermitCatalogue.catalogue[permit.name].Add(permit);
                    }
                }
            }

            //if (PermitCatalogue.catalogue.Count < 1)
            //{
            //    SSE.InvokeStandardError(SSE.StandardError.ERROR_11);
            //}
        }


        public static bool Add(string filePath, bool saving = true)
        {
            return PermitCatalogue.Add(new FileInfo(filePath), saving);
        }

        public static bool Add(FileInfo file, bool saving = true)
        {
            if (file.Exists && (file.Name.ToUpper() == "PERMIT.TXT"))
            {
                using (StreamReader reader = new StreamReader(file.OpenRead()))
                {
                    return PermitCatalogue.Add(reader, saving);
                }
            }
            else
            {
                SSE.InvokeStandardError(SSE.StandardError.ERROR_11);

                return false;
            }
        }

        private static bool Add(StreamReader reader, bool saving = true)
        {
            string? permitDateTime = reader.ReadLine();
            string? permitVersion = reader.ReadLine();
            string? permitType = reader.ReadLine();

            if (!string.IsNullOrEmpty(permitDateTime) &&
                !string.IsNullOrEmpty(permitVersion) &&
                !string.IsNullOrEmpty(permitType) &&
                permitDateTime.ToUpper().StartsWith(":DATE") &&
                permitVersion.ToUpper().StartsWith(":VERSION") &&
                permitType.ToUpper().StartsWith(":ENC"))
            {
                int addCount = 0;
                string? readLine = null;

                while ((readLine = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(readLine))
                    {
                        if (readLine.ToUpper().StartsWith(":ENC")) { break; }
                        if (readLine.ToUpper().StartsWith(":ECS")) { break; }

                        string[] dataSegment = readLine.Split(',');

                        if (dataSegment.Length > 0)
                        {
                            ENC.Permit? permit = null;

                            if (dataSegment[0].Length == 64)
                            {
                                string dataSection = dataSegment[0][..48];

                                permit = new ENC.Permit(dataSection[0..8]) {
                                    expirationDate = dataSection[8..16],
                                    key = (X: dataSection[16..32], Y: dataSection[32..]),
                                    checksum = dataSegment[0][48..],
                                };

                                if ((dataSegment.Length > 1) && int.TryParse(dataSegment[1], out int serviceLevel)) { permit.serviceLevel = serviceLevel; }
                                if ((dataSegment.Length > 2) && int.TryParse(dataSegment[2], out int EDTN)) { permit.EDTN = EDTN; }
                                if (dataSegment.Length > 3) { permit.DSID = dataSegment[3]; }
                                if (dataSegment.Length > 4) { permit.comment = dataSegment[4]; }

                                Cipher.BlowFish blowFish = new Cipher.BlowFish($"{PermitCatalogue.HWID}{PermitCatalogue.HWID[0]}");

                                if (Cipher.CRC.ValidateCRC(blowFish, dataSection, permit.checksum, true))
                                {
                                    (string? X, string? Y) decryptedKey = (
                                        X: blowFish.DecryptKey(Convert.FromHexString(permit.key.Value.X)),
                                        Y: blowFish.DecryptKey(Convert.FromHexString(permit.key.Value.Y))
                                    );

                                    if (!string.IsNullOrEmpty(decryptedKey.X) && !string.IsNullOrEmpty(decryptedKey.Y))
                                    {
                                        permit.key = (decryptedKey.X, decryptedKey.Y);
                                        permit.error = PermitCatalogue.Expiration(permit);

                                        switch (permit.error)
                                        {
                                            case ENC.Permit.Validation.Warning:
                                                {
                                                    SSE.InvokeStandardError(SSE.StandardError.ERROR_20, permit.name);
                                                }
                                                break;
                                            case ENC.Permit.Validation.Expired:
                                                {
                                                    SSE.InvokeStandardError(SSE.StandardError.ERROR_15, permit.name);
                                                    //SSE.InvokeStandardError(SSE.StandardError.ERROR_25, permit.name);
                                                }
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        permit.error = ENC.Permit.Validation.FatalError;
                                    }
                                }
                                else
                                {
                                    permit.error = ENC.Permit.Validation.FatalError;

                                    SSE.InvokeStandardError(SSE.StandardError.ERROR_13, permit.name);
                                }
                            }
                            else
                            {
                                if (dataSegment[0].Length > 7)
                                {
                                    permit = new ENC.Permit(dataSegment[0][0..8]) {
                                        error = ENC.Permit.Validation.FatalError,
                                    };

                                    SSE.InvokeStandardError(SSE.StandardError.ERROR_12, permit.name);
                                }
                                else
                                {
                                    SSE.InvokeStandardError(SSE.StandardError.ERROR_12);
                                }
                            }

                            if ((permit != null) && (permit.error != ENC.Permit.Validation.FatalError))
                            {
                                if (!PermitCatalogue.catalogue.ContainsKey(permit.name))
                                {
                                    PermitCatalogue.catalogue.Add(permit.name, new List<ENC.Permit>());
                                    PermitCatalogue.catalogue[permit.name].Add(permit);
                                }
                                else
                                {
                                    List<ENC.Permit> permitCatalogue = PermitCatalogue.catalogue[permit.name].Where(record => record.DSID == permit.DSID).ToList();

                                    if (permitCatalogue.Count > 0)
                                    {
                                        ENC.Permit selectedPermit = permit;

                                        PermitCatalogue.catalogue[permit.name].RemoveAll(record => record.DSID == permit.DSID);

                                        permitCatalogue.ForEach(cellPermit => {
                                            if (cellPermit.error != ENC.Permit.Validation.FatalError)
                                            {
                                                if (selectedPermit.error == ENC.Permit.Validation.FatalError)
                                                {
                                                    selectedPermit = cellPermit;
                                                }
                                                else
                                                {
                                                    bool sourceSuitable = DateTime.TryParseExact(selectedPermit.expirationDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime selectedExpirationDate);
                                                    bool referenceSuitable = DateTime.TryParseExact(cellPermit.expirationDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime referenceExpirationDate);

                                                    if (sourceSuitable && referenceSuitable)
                                                    {
                                                        if (selectedExpirationDate < referenceExpirationDate)
                                                        {
                                                            selectedPermit = cellPermit;
                                                        }
                                                    }
                                                    else if (!sourceSuitable && referenceSuitable)
                                                    {
                                                        selectedPermit = cellPermit;
                                                    }
                                                }
                                            }
                                        });

                                        PermitCatalogue.catalogue[permit.name].Add(selectedPermit);
                                    }
                                    else
                                    {
                                        PermitCatalogue.catalogue[permit.name].Add(permit);
                                    }
                                }

                                addCount++;
                            }
                        }
                        else
                        {
                            SSE.InvokeStandardError(SSE.StandardError.ERROR_12);
                        }
                    }
                }

                if (saving) { PermitCatalogue.Save(); }

                if (addCount < 1)
                {
                    SSE.InvokeStandardError(SSE.StandardError.ERROR_11);

                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                SSE.InvokeStandardError(SSE.StandardError.ERROR_11);

                return false;
            }
        }


        public static bool Delete(ENC.Permit permit, bool saving = true)
        {
            return PermitCatalogue.Delete(permit.name, permit.DSID, saving);
        }

        public static bool Delete(string name, string? dsid, bool saving = true)
        {
            if (PermitCatalogue.catalogue.ContainsKey(name))
            {
                List<ENC.Permit> deletePermit = PermitCatalogue.catalogue[name].Where(permit => permit.DSID == dsid).ToList();

                deletePermit.ForEach(permit => {
                    PermitCatalogue.catalogue[name].Remove(permit);
                });

                if (PermitCatalogue.catalogue[name].Count < 1)
                {
                    PermitCatalogue.catalogue.Remove(name);
                }

                if (saving) { PermitCatalogue.Save(); }

                return true;
            }
            else
            {
                return false;
            }
        }


        public static void Save()
        {
            FileInfo catalogueFile = new FileInfo(Path.Combine(DirectoryConfiguration.catalogue, PermitCatalogue.fileName));

            if (catalogueFile.Directory?.Exists == false) { catalogueFile.Directory.Create(); }

            PermitCatalogue.Save(catalogueFile.FullName);
        }

        public static void Save(string filePath)
        {
            PermitCatalogue.loaded = false;

            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                foreach (List<ENC.Permit> permitRecord in PermitCatalogue.catalogue.Values)
                {
                    permitRecord.ForEach(permit => PermitCatalogue.Write(writer, permit));
                }
            }

            PermitCatalogue.loaded = true;
        }

        public static void Save(Stream fileStream)
        {
            PermitCatalogue.loaded = false;

            using (StreamWriter writer = new StreamWriter(fileStream))
            {
                foreach (List<ENC.Permit> permitRecord in PermitCatalogue.catalogue.Values)
                {
                    permitRecord.ForEach(permit => PermitCatalogue.Write(writer, permit));
                }
            }

            PermitCatalogue.loaded = true;
        }

        private static void Write(StreamWriter writer, ENC.Permit permit)
        {
            writer.Write(permit.name); writer.Write('@');
            writer.Write(permit.error); writer.Write('@');
            writer.Write(permit.type); writer.Write('@');
            writer.Write(permit.checksum); writer.Write('@');
            writer.Write(permit.expirationDate); writer.Write('@');
            writer.Write(permit.key?.X); writer.Write('@');
            writer.Write(permit.key?.Y); writer.Write('@');
            writer.Write(permit.comment); writer.Write('@');
            writer.Write(permit.DSID); writer.Write('@');
            writer.Write(permit.EDTN); writer.Write('@');
            writer.WriteLine(permit.serviceLevel);
            writer.Flush();
        }


        private static ENC.Permit.Validation Expiration(ENC.Permit permit)
        {
            if (permit.error == ENC.Permit.Validation.FatalError) { return ENC.Permit.Validation.FatalError; }

            if (!string.IsNullOrEmpty(permit.expirationDate) &&
                DateTime.TryParseExact(permit.expirationDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime expirationDate))
            {
                DateTime now = DateTime.UtcNow;
                DateTime reference = expirationDate.ToUniversalTime();

                if (now > reference)
                {
                    permit.error = ENC.Permit.Validation.Expired;
                }
                else if ((reference - now).Days <= 30)
                {
                    permit.error = ENC.Permit.Validation.Warning;
                }
                else
                {
                    permit.error = ENC.Permit.Validation.Available;
                }
            }
            else
            {
                permit.error = ENC.Permit.Validation.Warning;
            }

            return permit.error.Value;
        }
    }
}