using Legacy.ECM_Core.Catalogue;
using Legacy.ECM_Core.Chart;
using Legacy.ECM_Core.Definition;
using System.IO;

namespace Legacy.ECM_Core.TDS
{
    public static class EncryptedTDS
    {
        public static bool Test_EncLicencing_2B(ECM_CORE ecm_core)
        {
            PermitCatalogue.Add_Permit("C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Encrypted_TDS\\2 ENC Licencing\\Test 2b\\PERMIT.TXT");

            return EncryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Encrypted_TDS\\2 ENC Licencing\\Test 2b");
        }

        public static bool Test_Authentication_Part1_4A(ECM_CORE ecm_core)
        {
            PermitCatalogue.Add_Permit("C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Encrypted_TDS\\4 Authentication_Part1\\Test 4a\\PERMIT.TXT");
            SaCatalogue.Add_SA("C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Encrypted_TDS\\4 Authentication_Part1\\Test 4a\\UKHO.CRT");

            return EncryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Encrypted_TDS\\4 Authentication_Part1\\Test 4a");
        }

        public static bool Test_EncDecryption_6A(ECM_CORE ecm_core)
        {
            PermitCatalogue.Add_Permit("C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Encrypted_TDS\\6 ENC Decryption\\Test 6a\\PERMIT.TXT");
            SaCatalogue.Add_SA("C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Encrypted_TDS\\6 ENC Decryption\\Test 6a\\IHO.CRT");

            return EncryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Encrypted_TDS\\6 ENC Decryption\\Test 6a");
        }

        public static bool Test_EncDecryption_6B(ECM_CORE ecm_core)
        {
            return EncryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Encrypted_TDS\\6 ENC Decryption\\Test 6b");
        }

        public static bool Test_DataExchangeMedia_8A(ECM_CORE ecm_core)
        {
            PermitCatalogue.Add_Permit("C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Encrypted_TDS\\8 Data Exchange Media\\Test 8a\\PERMIT.TXT");

            return EncryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Encrypted_TDS\\8 Data Exchange Media\\Test 8a");
        }

        public static bool Test_DataExchangeMedia_8C(ECM_CORE ecm_core)
        {
            PermitCatalogue.Add_Permit("C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Encrypted_TDS\\8 Data Exchange Media\\Test 8c\\PERMIT.TXT");

            bool Base_Result = EncryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Encrypted_TDS\\8 Data Exchange Media\\Test 8c\\BASE MEDIA");
            bool Update_Result = EncryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Encrypted_TDS\\8 Data Exchange Media\\Test 8c\\UPDATE MEDIA");

            return Base_Result && Update_Result;
        }


        public static bool Test(ECM_CORE ecm_core, string searching_directory)
        {
            DirectoryInfo Searching_DirectoryInfo = new DirectoryInfo(searching_directory);

            if (Searching_DirectoryInfo.Exists)
            {
                Dictionary<string, SearchChart> Search_Collection = ecm_core.Chart_Organizer.Search_S63(searching_directory).Where(Chart => Chart.Value.Necessary_Validation && Chart.Value.Product_Validation).ToDictionary();

                if (Search_Collection.Count > 0)
                {
                    ecm_core.Chart_Organizer.Import_Chart(Search_Collection);


                    DirectoryInfo FeatureAttribute_DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "ATTRIBUTE"));
                    DirectoryInfo SENC_DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "SENC"));
                    DirectoryInfo Search_DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "COVER"));
                    DirectoryInfo Update_DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "UPDATE"));

                    try
                    {
                        if (FeatureAttribute_DirectoryInfo.Exists) { FeatureAttribute_DirectoryInfo.Delete(true); }
                        if (SENC_DirectoryInfo.Exists) { SENC_DirectoryInfo.Delete(true); }
                        if (Search_DirectoryInfo.Exists) { Search_DirectoryInfo.Delete(true); }
                        if (Update_DirectoryInfo.Exists) { Update_DirectoryInfo.Delete(true); }
                    }
                    catch (Exception e)
                    {
                        return false;
                    }


                    bool Result = true;

                    foreach (string Chart in Search_Collection.Keys)
                    {
                        DetectionChart? Detection_Chart = ecm_core.Chart_Composer.Detect_Chart(Chart);

                        if (Detection_Chart != null)
                        {
                            if (ecm_core.Chart_Composer.Link_Chart(Detection_Chart))
                            {
                                if (ecm_core.Chart_Composer.Convert_Chart(Detection_Chart))
                                {

                                }
                                else
                                {
                                    Result = false;
                                }
                            }
                            else
                            {
                                Result = false;
                            }
                        }
                        else
                        {
                            Result = false;
                        }
                    }

                    ChartCatalogue.Save_Catalogue(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.SystemCatalogue_Directory, "CHART.cat"));

                    return Result;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}