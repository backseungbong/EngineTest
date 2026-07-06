using JHLib.ChartManager.Record;

namespace Legacy.ECM_Core.DCC
{
    public class Feature
    {
        public FRID FRID;
        public FOID FOID;

        public List<ATTF>? ATTF;
        public List<NATF>? NATF;

        public List<FFPT>? FFPT;
        public List<FSPT>? FSPT;
        public List<FFPC>? FFPC;
        public List<FSPC>? FSPC;


        public string Attribute = "";
        public (string Start, string End) Valid_Date = (Start: "", End: "");

        public byte Update_Type = 0;
        public bool Error = false;

        #region [[ Information ]]
        public string NATSUR = "";
        public string VERCCL = "";
        public string VERCOP = "";
        public string VERCLR = "";
        public string VERCSA = "";

        public string OBJNAM = "";
        public string NOBJNM = "";
        public string CURVEL = "";

        public string INFORM = "";
        public string NINFOM = "";

        public string ELEVAT = "";
        public string VALACM = "";
        public string VALMAG = "";
        public string COMCHA = "";
        public string ORIENT = "";

        public string TXTDSC = "";
        public string NTXTDS = "";

        public string PILDST = "";
        public string NPILDST = "";

        public string TXSTR = "";
        public string NTXST = "";

        public string DRVAL1 = "";

        public string CURSTR = "";
        public string LOCTIM = "";
        public string USRMRK = "";

        public string RYRMGV = "";
        #endregion

        public FeatureAttributeRecord? faRecord = null;



        public bool Get_ATVL(ushort attl, out string atvl)
        {
            bool Result = false;

            if (this.ATTF != null)
            {
                IEnumerable<string> ATVL_Enumeration = this.ATTF.Where(ATTF => (ATTF.ATTL == attl) && (ATTF.ATVL.Length > 0)).Select(ATTF => ATTF.ATVL[0]);

                if (ATVL_Enumeration.Count() > 0)
                {
                    atvl = ATVL_Enumeration.First();
                    Result = true;
                }
                else
                {
                    atvl = string.Empty;
                }
            }
            else
            {
                atvl= string.Empty;
            }

            return Result;
        }

        public (string Text, string NationalText, int Type) Get_AttributeInformation(string acronym)
        {
            (string Text, string NationalText, int Type) Result;

            switch (acronym)
            {
                case string _ when acronym.Contains("NATSUR"): { Result = (Text: this.NATSUR, NationalText: "", Type: 0); } break;
                case string _ when acronym.Contains("VERCCL"): { Result = (Text: this.VERCCL, NationalText: "", Type: 1); } break;
                case string _ when acronym.Contains("VERCOP"): { Result = (Text: this.VERCOP, NationalText: "", Type: 1); } break;
                case string _ when acronym.Contains("VERCLR"): { Result = (Text: this.VERCLR, NationalText: "", Type: 1); } break;
                case string _ when acronym.Contains("VERCSA"): { Result = (Text: this.VERCSA, NationalText: "", Type: 1); } break;
                case string _ when acronym.Contains("CURVEL"): { Result = (Text: this.CURVEL, NationalText: "", Type: 1); } break;
                case string _ when acronym.Contains("INFORM"): { Result = (Text: this.INFORM, NationalText: this.INFORM, Type: 0); } break;
                case string _ when acronym.Contains("ELEVAT"): { Result = (Text: this.ELEVAT, NationalText: "", Type: 1); } break;
                case string _ when acronym.Contains("VALMAG"): { Result = (Text: this.VALMAG, NationalText: "", Type: 1); } break;
                case string _ when acronym.Contains("COMCHA"): { Result = (Text: this.COMCHA, NationalText: "", Type: 0); } break;
                case string _ when acronym.Contains("ORIENT"): { Result = (Text: this.ORIENT, NationalText: "", Type: 1); } break;
                case string _ when acronym.Contains("OBJNAM"): { Result = (Text: this.OBJNAM, NationalText: this.NOBJNM, Type: 0); } break;
                case string _ when acronym.Contains("PILDST"): { Result = (Text: this.PILDST, NationalText: this.NPILDST, Type: 0); } break;
                case string _ when acronym.Contains("TXTDSC"): { Result = (Text: this.TXTDSC, NationalText: this.NTXTDS, Type: 0); } break;
                case string _ when acronym.Contains("$TXSTR"): { Result = (Text: this.TXSTR, NationalText: this.NTXST, Type: 0); } break;
                case string _ when acronym.Contains("DRVAL1"): { Result = (Text: this.DRVAL1, NationalText: "", Type: 1); } break;
                case string _ when acronym.Contains("curstr"): { Result = (Text: this.CURSTR, NationalText: "", Type: 0); } break;
                case string _ when acronym.Contains("loctim"): { Result = (Text: this.LOCTIM, NationalText: "", Type: 0); } break;
                case string _ when acronym.Contains("usrmrk"): { Result = (Text: this.USRMRK, NationalText: "", Type: 0); } break;
                case string _ when acronym.Contains("<unknown>"):
                case string _ when acronym.Contains("9999"):
                default: { Result = (Text: "", NationalText: "", Type: 0); } break;
            }

            return Result;
        }

        public void Set_AttributeInformation(ushort attl, string information)
        {
            switch (attl)
            {
                case 113: { this.NATSUR = information; } break;
                case 181: { this.VERCLR = information; } break;
                case 182: { this.VERCCL = information; } break;
                case 183: { this.VERCOP = information; } break;
                case 184: { this.VERCSA = information; } break;
                case 116: { this.OBJNAM = information; } break;
                case 84: { this.CURVEL = information; } break;
                case 102: { this.INFORM = information; } break;
                case 90: { this.ELEVAT = information; } break;
                case 173: { this.VALACM = information; } break;
                case 176: { this.VALMAG = information; } break;
                case 77: { this.COMCHA = information; } break;
                case 117: { this.ORIENT = information; } break;
                case 121: { this.PILDST = information; } break;
                case 158: { this.TXTDSC = information; } break;
                case 157: { this.NTXTDS = information; } break;
                case 303: { this.NTXST = information; } break;
                case 301: { this.NOBJNM = information; } break;
                case 300: { this.NINFOM = information; } break;
                case 302: { this.NPILDST = information; } break;
                case 304: { this.NTXTDS = information; } break;
                case 87: { this.DRVAL1 = information; } break;
                case 8199: { this.CURSTR = information; } break;
                case 8202: { this.LOCTIM = information; } break;
                case 8214: { this.USRMRK = information; } break;
                case 130: { this.RYRMGV = information; } break;
            }
        }


        public void ModifyUpdate_FSPC(Feature update)
        {
            if (update.FSPC == null) { return; }


            foreach (FSPC Update_FSPC in update.FSPC)
            {
                if ((Update_FSPC.FSUI != 1) && !(this.FSPT?.Count > 0)) { continue; }

                switch (Update_FSPC.FSUI)
                {
                    case 1:
                        for (int i = 0; i < Update_FSPC.NSPT; i++)
                        {
                            int j = Update_FSPC.FSIX - 1 + i;

                            if (j >= 0)
                            {
                                if ((i < update.FSPT?.Count) && (j <= (this.FSPT?.Count ?? 0)))
                                {
                                    this.FSPT ??= new List<FSPT>();
                                    this.FSPT.Insert(j, update.FSPT[i]);
                                }
                            }
                        }
                        break;
                    case 2:
                        for (int i = Update_FSPC.NSPT - 1; i >= 0; i--)
                        {
                            int j = Update_FSPC.FSIX - 1 + i;

                            if (j >= 0)
                            {
                                if (this.FSPT?.Count < j)
                                {
                                    this.FSPT.RemoveAt(j);
                                }
                            }
                        }
                        break;
                    case 3:
                        for (int i = 0; i < Update_FSPC.NSPT; i++)
                        {
                            int j = Update_FSPC.FSIX - 1 + i;

                            if (j >= 0)
                            {
                                if ((i < update.FSPT?.Count) && (j < this.FSPT?.Count))
                                {
                                    this.FSPT[j] = update.FSPT[i];
                                }
                            }
                        }
                        break;
                }
            }
        }

        public void ModifyUpdate_FFPT(Feature update)
        {
            if (update.FFPT?.Count > 0)
            {
                this.FFPT = new List<FFPT>(update.FFPT);
            }

            if (update.FFPC?.Count > 0)
            {
                this.FFPC = new List<FFPC>(update.FFPC);
            }
        }

        public void ModifyUpdate_ATTF(Feature update)
        {
            if (update.ATTF == null) { return; }


            foreach (ATTF Update_ATTF in update.ATTF)
            {
                if (this.ATTF?.Count > 0)
                {
                    int Target = this.ATTF.FindIndex(ATTF => ATTF.ATTL == Update_ATTF.ATTL);

                    if (Target > -1)
                    {
                        this.ATTF[Target] = Update_ATTF;
                    }
                }
                else
                {
                    this.ATTF ??= new List<ATTF>();
                    this.ATTF.Add(Update_ATTF);
                }
            }
        }

        public void ModifyUpdate_NATF(Feature update)
        {
            if (update.NATF == null) { return; }


            foreach (NATF Update_NATF in update.NATF)
            {
                if (this.NATF?.Count > 0)
                {
                    int Target = this.NATF.FindIndex(NATF => NATF.ATTL == Update_NATF.ATTL);

                    if (Target > -1)
                    {
                        this.NATF[Target] = Update_NATF;
                    }
                }
                else
                {
                    this.NATF ??= new List<NATF>();
                    this.NATF.Add(Update_NATF);
                }
            }
        }


        public bool Is_HighlightDocument()
        {
            bool Result = false;

            if (this.ATTF != null)
            {
                IEnumerable<ATTF> ATTF_Enumeration = this.ATTF.Where(ATTF => (ATTF.ATTL == 158) || (ATTF.ATTL == 304) || (ATTF.ATTL == 120));

                if (ATTF_Enumeration.Count() > 0)
                {
                    Result = true;
                }
            }

            return Result;
        }

        public bool Has_INFORM()
        {
            bool Result = false;

            if (this.ATTF != null)
            {
                IEnumerable<ATTF> ATTF_Enumeration = this.ATTF.Where(ATTF => (ATTF.ATTL == 102) || (ATTF.ATTL == 300));

                if (ATTF_Enumeration.Count() > 0)
                {
                    Result = true;
                }
            }

            return Result;
        }

        public bool Is_Reverse()
        {
            bool Result;

            if (this.FRID.PRIM == 2)
            {
                bool ORIENT = false;

                if (this.FSPT != null)
                {
                    IEnumerable<FSPT> FSPT_Enumeration = this.FSPT.Where(FSPT => FSPT.ORNT == 2);

                    if (FSPT_Enumeration.Count() > 0)
                    {
                        ORIENT = true;
                    }
                }

                Result = ORIENT;
            }
            else
            {
                Result = false;
            }

            return Result;
        }

        public bool Is_Reverse(int i)
        {
            bool Result;

            if (this.FRID.PRIM == 2)
            {
                bool ORIENT = false;

                if (this.FSPT != null)
                {
                    ORIENT = (this.FSPT[i].ORNT == 2);
                }

                Result = ORIENT;
            }
            else
            {
                Result = false;
            }

            return Result;
        }
    }
}