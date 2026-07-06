using Legacy.ECM_Core.Catalogue;
using Legacy.ECM_Core.Chart;
using Legacy.ECM_Core.Component;
using Legacy.ECM_Core.Definition;
using Legacy.ECM_Core.Table;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Legacy.ECM_Core
{
    public partial class ECM_CORE
    {
        public bool Search_Chart(string searching_directory, out Dictionary<string, SearchChart>? search_collection)
        {
            if (Directory.Exists(searching_directory))
            {
                FileInfo[] Search_File = new DirectoryInfo(searching_directory).GetFiles("*", SearchOption.AllDirectories);
                IEnumerable<FileInfo> SearchSerial_Enumeration = Search_File.Where(File => File.Name.Contains("SERIAL.ENC"));

                search_collection = (SearchSerial_Enumeration.Count() > 0) ? Chart_Organizer.Search_S63(searching_directory) : Chart_Organizer.Search_S57(searching_directory);

                return true;
            }
            else
            {
                search_collection = null;

                return false;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="search_collection"></param>
        /// <param name="asynchronizing"></param>
        /// <returns>Report of Install Results</returns>
        /// <exception cref="InaccessibleException"></exception>
        public Dictionary<string, InstallReport> Install_Chart(Dictionary<string, SearchChart> search_collection, bool asynchronizing = false)
        {
            if (!CoreControl_Accessible) { throw new InaccessibleException("Core Control was Inaccessible"); }

            CoreControl_Accessible = false;


            Dictionary<string, InstallReport> Install_Report = new Dictionary<string, InstallReport>();

            Chart_Organizer.Import_Chart(search_collection);

            if (asynchronizing)
            {
                int Schedule_Size = Math.Min(search_collection.Count, Asynchronizing_Size);

                Task<InstallReport>[] Install_Schedule = new Task<InstallReport>[Schedule_Size];
                string[] ChartName_Collection = search_collection.Keys.ToArray();

                int Schedule = 0;
                int Complete = 0;

                for (int i = 0; i < Schedule_Size; i++)
                {
                    string Chart_Name = ChartName_Collection[Schedule];

                    Install_Schedule[i] = Task.Run(() => {
                        return Install_Chart(Chart_Name);
                    });

                    Schedule++;
                }


                while (Schedule < search_collection.Count)
                {
                    int Index = Task.WaitAny(Install_Schedule);

                    Task<InstallReport> Completed_Schedule = Install_Schedule[Index];
                    InstallReport Report = Completed_Schedule.Result;

                    if (Completed_Schedule.IsFaulted || Completed_Schedule.IsCanceled)
                    {
                        Report.Result = false;

                        if (Completed_Schedule.IsFaulted) { Report.Reason = "Task is Faulted"; }
                        if (Completed_Schedule.IsCanceled) { Report.Reason = "Task is Canceled"; }
                    }

                    Install_Report.Add(Report.Chart, Report);

                    Invoke_ChartInstall_Message(Report);


                    string Chart_Name = ChartName_Collection[Schedule];

                    Install_Schedule[Index] = Task.Run(() => {
                        return Install_Chart(Chart_Name);
                    });

                    Schedule++;
                    Complete++;
                }


                while (Complete < search_collection.Count)
                {
                    int Index = Task.WaitAny(Install_Schedule); // 마지막에 문제가 Schedule에 완료된 거 남아 있으니까 계속 그거만 나옴

                    Task<InstallReport> Completed_Schedule = Install_Schedule[Index];
                    InstallReport Report = Completed_Schedule.Result;

                    if (Completed_Schedule.IsFaulted || Completed_Schedule.IsCanceled)
                    {
                        Report.Result = false;

                        if (Completed_Schedule.IsFaulted) { Report.Reason = "Task is Faulted"; }
                        if (Completed_Schedule.IsCanceled) { Report.Reason = "Task is Canceled"; }
                    }

                    Install_Report.Add(Report.Chart, Report);

                    Invoke_ChartInstall_Message(Report);


                    int Current_Size = Install_Schedule.Length; // WaitAny가 null 허용을 안 하는 거 같으니 어쩔 수 없이 Array를 새로 만들어내야 할 듯 (여기는 마지막 Waiting 이라서 부담되는 작업은 아닐 거 같음. 기존 사이즈가 10이므로 최대 10번만 할 거니까.)
                    int Input_Index = 0;

                    Task<InstallReport>[] Waiting_Schedule = new Task<InstallReport>[Current_Size - 1];

                    for (int i = 0; i < Install_Schedule.Length; i++)
                    {
                        if (i != Index)
                        {
                            Waiting_Schedule[Input_Index++] = Install_Schedule[i];
                        }
                    }

                    Install_Schedule = Waiting_Schedule;

                    Complete++;
                }
            }
            else
            {
                foreach (string Chart in search_collection.Keys)
                {
                    InstallReport Report = Install_Chart(Chart);
                    Install_Report.Add(Report.Chart, Report);

                    Invoke_ChartInstall_Message(Report);
                }
            }

            if (!Using_Chart1)
            {
                ChartCatalogue.Save_Catalogue(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.SystemCatalogue_Directory, "CHART.cat"));
            }
            else
            {
                Chart1Catalogue.Save_Catalogue(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.SystemCatalogue_Directory, "CHART_ONE.cat"));
            }


            CoreControl_Accessible = true;

            return Install_Report;
        }

        internal InstallReport Install_Chart(string chart_name)
        {
            DetectionChart? Detection_Chart = Chart_Composer.Detect_Chart(chart_name);

            if (Detection_Chart != null)
            {
                if (Chart_Composer.Link_Chart(Detection_Chart))
                {
                    if (Chart_Composer.Convert_Chart(Detection_Chart))
                    {
                        InstallReport Report = new InstallReport();
                        Report.Chart = chart_name;
                        Report.Result = true;
                        Report.Reason = "Successed";

                        return Report;
                    }
                    else
                    {
                        InstallReport Report = new InstallReport();
                        Report.Chart = chart_name;
                        Report.Result = false;
                        Report.Reason = "Failed Chart Converting";

                        return Report;
                    }
                }
                else
                {
                    InstallReport Report = new InstallReport();
                    Report.Chart = chart_name;
                    Report.Result = false;
                    Report.Reason = "Failed Chart Linking";

                    return Report;
                }
            }
            else
            {
                InstallReport Report = new InstallReport();
                Report.Chart = chart_name;
                Report.Result = false;
                Report.Reason = "Failed Chart Detection";

                return Report;
            }
        }

        internal void Invoke_ChartInstall_Message(InstallReport report)
        {
            Task.Run(() => {
                Trace.WriteLine($"[{report.Chart}] {report.Reason}");
                Reported_Install?.Invoke(report);
            });
        }


        public Stream? Get_ResourceAttributeCatalogue()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("JHLib.ChartManager.Legacy.Resource.ATTRIBUTE.cat");
        }

        public Stream? Get_ResourceObjectCatalogue()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("JHLib.ChartManager.Legacy.Resource.OBJECT.cat");
        }

        public void Read_ResourceAttributeCatalogue()
        {
            using (Stream? Resource_AttributeCatalogue = Get_ResourceAttributeCatalogue())
            {
                if (Resource_AttributeCatalogue != null) { AttributeCatalogue.Read(Resource_AttributeCatalogue); }
            }
        }

        public void Read_ResourceObjectCatalogue()
        {
            using (Stream? Resource_ObjectCatalogue = Get_ResourceObjectCatalogue())
            {
                if (Resource_ObjectCatalogue != null) { ObjectCatalogue.Read(Resource_ObjectCatalogue); }
            }
        }


        public Stream? Get_ResourceColorTable()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("JHLib.ChartManager.Legacy.Resource.COLOR_ACRONYM.txt");
        }

        public void Read_ResourceColorTable()
        {
            using (Stream? Resource_ColorTable = Get_ResourceColorTable())
            {
                if (Resource_ColorTable != null) { ColorTable.Read(Resource_ColorTable); }
            }
        }


        public Stream? Get_ResourceSymbolTable()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("JHLib.ChartManager.Legacy.Resource.SYMBOL_ACRONYM.inf");
        }

        public Stream? Get_ResourceLineTable()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("JHLib.ChartManager.Legacy.Resource.LINE_ACRONYM.inf");
        }

        public Stream? Get_ResourcePatternTable()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("JHLib.ChartManager.Legacy.Resource.PATTERN_ACRONYM.inf");
        }

        public void Read_ResourceFigureTable()
        {
            using (Stream? Resource_SymbolTable = Get_ResourceSymbolTable())
            {
                if (Resource_SymbolTable != null) { SymbolTable.Read(Resource_SymbolTable); }
            }

            using (Stream? Resource_LineTable = Get_ResourceLineTable())
            {
                if (Resource_LineTable != null) { LineTable.Read(Resource_LineTable); }
            }

            using (Stream? Resource_PatternTable = Get_ResourcePatternTable())
            {
                if (Resource_PatternTable != null) { PatternTable.Read(Resource_PatternTable); }
            }
        }


        public Stream? Get_ResourcePPTable()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("JHLib.ChartManager.Legacy.Resource.LOOKUP_P_P.lut");
        }

        public Stream? Get_ResourcePSTable()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("JHLib.ChartManager.Legacy.Resource.LOOKUP_P_S.lut");
        }

        public Stream? Get_ResourceLTable()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("JHLib.ChartManager.Legacy.Resource.LOOKUP_L.lut");
        }

        public Stream? Get_ResourceAPTable()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("JHLib.ChartManager.Legacy.Resource.LOOKUP_A_P.lut");
        }

        public Stream? Get_ResourceASTable()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("JHLib.ChartManager.Legacy.Resource.LOOKUP_A_S.lut");
        }

        public void Read_ResourceLookupTable()
        {
            using (Stream? Resource_PP_Table = Get_ResourcePPTable())
            {
                if (Resource_PP_Table != null) { PPTable.Read(Resource_PP_Table); }
            }

            using (Stream? Resource_PS_Table = Get_ResourcePSTable())
            {
                if (Resource_PS_Table != null) { PSTable.Read(Resource_PS_Table); }
            }

            using (Stream? Resource_L_Table = Get_ResourceLTable())
            {
                if (Resource_L_Table != null) { LTable.Read(Resource_L_Table); }
            }

            using (Stream? Resource_AP_Table = Get_ResourceAPTable())
            {
                if (Resource_AP_Table != null) { APTable.Read(Resource_AP_Table); }
            }

            using (Stream? Resource_AS_Table = Get_ResourceASTable())
            {
                if (Resource_AS_Table != null) { ASTable.Read(Resource_AS_Table); }
            }
        }


        public Stream? Get_Chart1ResourcePPTable()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("JHLib.ChartManager.Legacy.Resource.Chart1.LOOKUP_P_P.txt");
        }

        public Stream? Get_Chart1ResourcePSTable()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("JHLib.ChartManager.Legacy.Resource.Chart1.LOOKUP_P_S.txt");
        }

        public Stream? Get_Chart1ResourceLTable()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("JHLib.ChartManager.Legacy.Resource.Chart1.LOOKUP_L.txt");
        }

        public Stream? Get_Chart1ResourceATable()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("JHLib.ChartManager.Legacy.Resource.Chart1.LOOKUP_A.txt");
        }

        public void Read_Chart1ResourceLookupTable()
        {
            using (Stream? Chart1Resource_PP_Table = Get_Chart1ResourcePPTable())
            {
                if (Chart1Resource_PP_Table != null) { PPTable.Read(Chart1Resource_PP_Table); }
            }

            using (Stream? Chart1Resource_PS_Table = Get_Chart1ResourcePSTable())
            {
                if (Chart1Resource_PS_Table != null) { PSTable.Read(Chart1Resource_PS_Table); }
            }

            using (Stream? Chart1Resource_L_Table = Get_Chart1ResourceLTable())
            {
                if (Chart1Resource_L_Table != null) { LTable.Read(Chart1Resource_L_Table); }
            }

            using (Stream? Chart1Resource_A_Table = Get_Chart1ResourceATable())
            {
                if (Chart1Resource_A_Table != null) { ATable.Read(Chart1Resource_A_Table); }
            }
        }


        public Stream? Get_ResourceSchemeAdministrator()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("JHLib.ChartManager.Legacy.Resource.SA.txt");
        }

        public Stream? Get_ResourceCellPermit()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("JHLib.ChartManager.Legacy.Resource.CP.txt");
        }

        public void Read_ResourceSchemeAdministrator()
        {
            using (Stream? Resource_SaCatalogue = Get_ResourceSchemeAdministrator())
            {
                if (Resource_SaCatalogue != null) { SaCatalogue.Read(Resource_SaCatalogue); }
            }
        }

        public void Read_ResourceCellPermit()
        {
            using (Stream? Resource_PermitCatalogue = Get_ResourceCellPermit())
            {
                if (Resource_PermitCatalogue != null) { PermitCatalogue.Read(Resource_PermitCatalogue); }
            }
        }


        public void Set_UsingChart1(bool chart1, bool read_resource)
        {
            if (CoreControl_Accessible)
            {
                if (read_resource)
                {
                    if (chart1)
                    {
                        Read_Chart1ResourceLookupTable();
                    }
                    else
                    {
                        Read_ResourceLookupTable();
                    }
                }

                Chart_Composer.Using_Chart1 = chart1;
                Using_Chart1 = chart1;
            }
        }

        public void Set_AsynchronizingSize(int size)
        {
            if (CoreControl_Accessible)
            {
                Asynchronizing_Size = (size < 1) ? 1 : size;
            }
        }
    }
}