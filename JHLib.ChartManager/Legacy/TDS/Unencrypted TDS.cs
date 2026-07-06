using Legacy.ECM_Core.Catalogue;
using Legacy.ECM_Core.Chart;
using Legacy.ECM_Core.Definition;
using System.IO;

namespace Legacy.ECM_Core.TDS
{
    public static class UnencryptedTDS
    {
        public static bool Test_PowerUp(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\2.1.1 Power Up\\ENC_ROOT");
        }

        public static bool Test_LoadingCorruptData(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\2.1.5 Loading Corrupt Data\\ENC_ROOT");
        }

        public static bool Test_CorruptUpdate(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\2.2.1 Corrupt Update\\ENC_ROOT");
        }

        public static bool Test_LoadingOfUpdates(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\2.2.2 Loading of Updates\\ENC_ROOT");
        }

        //public static bool Test_LoadingOfInvalidSequence(ECM_CORE ecm_core) // 폴더가 다 나뉘어져 있는 형태인데, 그러면 S-57도 지정된 Directory에서 모든 ENC 찾도록 해야하는데. 아니면 함수를 쓸 때 여러번 쓸 수 있게 해야.
        //{
            
        //}

        public static bool Test_LoadingOfNewUpdate(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\2.2.4 Loading of New Update\\ENC_ROOT");
        }

        public static bool Test_GoodBaseCells(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\2.2.5 Good Base Cells\\ENC_ROOT");
        }

        public static bool Test_OldUpdate(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\2.2.5 Old Update\\ENC_ROOT");
        }

        //public static bool Test_ReIssue(ECM_CORE ecm_core) // 폴더 나뉘어 있음
        //{
            
        //}

        //public static bool Test_Cancellation(ECM_CORE ecm_core) // cancel chart는 제대로 고려 안 하고 만든 상태라 일단 건너뜀
        //{

        //}

        public static bool Test_EncDisplay_Base(ECM_CORE ecm_core) // 폴더 나뉘어 있는데, test 함수 senc clear 시키고 해버리고 있어서, 한 번에 여러 폴더 쓰는 걸 만들어야 할 듯?
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\3.1 ENC Display\\Base\\ENC_ROOT");
        }

        public static bool Test_EncDisplay_Standard(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\3.1 ENC Display\\Standard\\ENC_ROOT");
        }

        public static bool Test_EncDisplay_Other(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\3.1 ENC Display\\Other\\ENC_ROOT");
        }

        public static bool Test_InvalidObject(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\3.2 Invalid Object\\ENC_ROOT");
        }

        public static bool Test_InvalidObject_InvalidBase(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\3.2 Invalid Object\\Invalid Base\\ENC_ROOT");
        }

        public static bool Test_Settings(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\3.3 Settings\\ENC_ROOT");
        }

        public static bool Test_NonOfficialData(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\3.4 Non-Official Data\\ENC_ROOT");
        }

        public static bool Test_DisplayPriorities(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\3.6 Display Priorities\\ENC_ROOT");
        }

        public static bool Test_Overlap(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\3.7 Overlap\\ENC_ROOT");
        }

        public static bool Test_ScaleMinimum(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\3.7.7 Scale minimum\\ENC_ROOT");
        }

        public static bool Test_NonEncData(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\3.8.5 Non ENC data\\ENC_ROOT");
        }

        public static bool Test_PolarEncData(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\3.9 Polar ENC Data\\ENC_ROOT");
        }

        public static bool Test_NavigationalHazards(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\5.0 Navigational Hazards\\ENC_ROOT");
        }

        public static bool Test_NavigationalHazards_Overview(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\5.0 Navigational Hazards\\Overview\\ENC_ROOT");
        }

        public static bool Test_SpecialConditions(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\6.0 Special Conditions\\ENC_ROOT");
        }

        public static bool Test_SafetyContour(ECM_CORE ecm_core)
        {
            return UnencryptedTDS.Test(ecm_core, "C:\\Users\\user\\Documents\\2023\\Electronic Chart Manager\\보관\\S-64_ENC_Unencrypted_TDS\\7.0 Safety Contour\\ENC_ROOT");
        }


        public static bool Test(ECM_CORE ecm_core, string searching_directory)
        {
            DirectoryInfo Searching_DirectoryInfo = new DirectoryInfo(searching_directory);

            if (Searching_DirectoryInfo.Exists)
            {
                Dictionary<string, SearchChart> Search_Collection = ecm_core.Chart_Organizer.Search_S57(Searching_DirectoryInfo.FullName);

                if (Search_Collection.Count > 0)
                {
                    ecm_core.Chart_Organizer.Import_Chart(Search_Collection);


                    DirectoryInfo FeatureAttribute_DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "ATTRIBUTE"));
                    DirectoryInfo SENC_DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "SENC"));
                    DirectoryInfo Search_DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "COVERAGE"));
                    DirectoryInfo Detection_DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "DETECTION"));
                    DirectoryInfo Update_DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "UPDATE"));

                    try
                    {
                        if (FeatureAttribute_DirectoryInfo.Exists) { FeatureAttribute_DirectoryInfo.Delete(true); }
                        if (SENC_DirectoryInfo.Exists) { SENC_DirectoryInfo.Delete(true); }
                        if (Search_DirectoryInfo.Exists) { Search_DirectoryInfo.Delete(true); }
                        if (Detection_DirectoryInfo.Exists) { Detection_DirectoryInfo.Delete(true); }
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