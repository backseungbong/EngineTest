using System.Text.RegularExpressions;
using System.Text;
using System.IO;

namespace Legacy.ECM_Core.Chart
{
    public class DetectionCell
    {
        public DCC.DSID DSID { get; internal set; }
        public DCC.DSSI DSSI { get; internal set; }
        public DCC.DSPM DSPM { get; internal set; }

        public List<DCC.Vector>? VI { get; internal set; } // Isolate Node
        public List<DCC.Vector>? VC { get; internal set; } // Connected Node
        public List<DCC.Vector>? VE { get; internal set; } // Edge

        public List<DCC.Feature>? Feature { get; internal set; }

        public (double North, double South, double East, double West) Boundary { get; internal set; }

        public int Edition_Number { get; protected set; } = -1;
        public int Update_Number { get; protected set; } = -1;

        public bool Absorbed { get; internal set; } = false;



        public DetectionCell()
        {
            this.DSID = new DCC.DSID();
            this.DSSI = new DCC.DSSI();
            this.DSPM = new DCC.DSPM();
        }

        public DetectionCell(DCC.DSPM DSPM)
        {
            this.DSID = new DCC.DSID();
            this.DSSI = new DCC.DSSI();
            this.DSPM = DSPM;
        }



        public void Read(string file_path)
        {
            this.VI = null;
            this.VC = null;
            this.VE = null;
            this.Feature = null;
            this.Edition_Number = -1;
            this.Update_Number = -1;

            using (FileStream Cell_Stream = new FileStream(file_path, FileMode.Open, FileAccess.Read))
            using (BinaryReader Cell_Reader = new BinaryReader(Cell_Stream))
            {
                Read(Cell_Reader);
            }
        }

        public void Read(Stream file_stream)
        {
            this.VI = null;
            this.VC = null;
            this.VE = null;
            this.Feature = null;
            this.Edition_Number = -1;
            this.Update_Number = -1;

            using (BinaryReader Cell_Reader = new BinaryReader(file_stream))
            {
                Read(Cell_Reader);
            }
        }

        private void Read(BinaryReader cell_reader)
        {
            if (cell_reader.BaseStream.Length < 5) { return; }


            byte[] DSGI_Index = [
                cell_reader.ReadByte(),
                cell_reader.ReadByte(),
                cell_reader.ReadByte(),
                cell_reader.ReadByte(),
                cell_reader.ReadByte(),
            ];


            //Regex Header_Regex = new Regex(@"[0-9]{4};&.{2} .+");
            Regex Category_Regex = new Regex(@"[0-9]{5} D     [0-9]{5}   .+");

            List<(string Tag_Name, byte[] Data_Storage)> Tag_List = new List<(string Tag_Name, byte[] Data_Storage)>();

            DCC.Vector? Vector_Builder = null;
            DCC.Feature? Feature_Builder = null;

            byte Read = 0;
            bool Category_Line = false;

            while (cell_reader.BaseStream.Position != cell_reader.BaseStream.Length)
            {
                if (!Category_Line)
                {
                    StringBuilder Category_Builder = new StringBuilder();

                    while (cell_reader.BaseStream.Position != cell_reader.BaseStream.Length)
                    {
                        Read = cell_reader.ReadByte();
                        
                        if (Read == 0x1E) { break; }

                        Category_Builder.Append((char)Read);
                    }


                    string Category_Sentence = Category_Builder.ToString();

                    if (Category_Regex.Match(Category_Sentence).Success)
                    {
                        Category_Line = true;

                        Tag_List.Clear();

                        Vector_Builder = null;
                        Feature_Builder = null;

                        if (Category_Sentence.Length > 24)
                        {
                            string Tag_Line = Category_Sentence[24..];

                            int LF = Category_Sentence[20] - 48;
                            int PF = Category_Sentence[21] - 48;
                            int TF = Category_Sentence[23] - 48;

                            int Tag_Size = LF + PF + TF;
                            int Tag_Count = Tag_Line.Length / Tag_Size;

                            for (int i = 0; i < Tag_Count; i++)
                            {
                                int Tag_Index = Tag_Size * i;
                                string Tag_Name = Tag_Line.Substring(Tag_Index, TF);

                                int Data_Size = 0;

                                for (int j = 0; j < LF; j++)
                                {
                                    int Digits = 1;

                                    for (int k = 0; k < j; k++)
                                    {
                                        Digits *= 10;
                                    }

                                    Data_Size += (Tag_Line[Tag_Index + Tag_Size - 1 - PF - j] - 48) * Digits;
                                }

                                Tag_List.Add((Tag_Name, new byte[Data_Size]));
                            }
                        }
                    }
                }
                else
                {
                    foreach ((string Tag_Name, byte[] Data_Storage) Tag in Tag_List)
                    {
                        for (int i = 0; i < Tag.Data_Storage.Length; i++)
                        {
                            if (cell_reader.BaseStream.Position != cell_reader.BaseStream.Length)
                            {
                                Tag.Data_Storage[i] = cell_reader.ReadByte();
                            }
                            else
                            {
                                break;
                            }
                        }


                        bool Record_Separator = false;

                        if ((Tag.Tag_Name == "NATF") && (this.DSSI.NALL == 2))
                        {
                            Record_Separator = ((Tag.Data_Storage[^2] == 0x1E) && (Tag.Data_Storage[^1] == 0x00));
                        }
                        else
                        {
                            Record_Separator = (Tag.Data_Storage[^1] == 0x1E);
                        }

                        if (Record_Separator)
                        {
                            Read_Element(Tag.Tag_Name, Tag.Data_Storage, ref Vector_Builder, ref Feature_Builder);
                        }
                    }


                    switch (Vector_Builder?.VRID.RCNM)
                    {
                        case 110:
                            {
                                this.VI ??= new List<DCC.Vector>();
                                this.VI.Add(Vector_Builder);
                            }
                            break;
                        case 120:
                            {
                                this.VC ??= new List<DCC.Vector>();
                                this.VC.Add(Vector_Builder);
                            }
                            break;
                        case 130:
                            {
                                this.VE ??= new List<DCC.Vector>();
                                this.VE.Add(Vector_Builder);
                            }
                            break;
                    }

                    if (Feature_Builder?.FRID.RCID > 0)
                    {
                        if (Feature_Builder.FRID.OBJL > 646)
                        {
                            if ((8193 <= Feature_Builder.FRID.OBJL) && (Feature_Builder.FRID.OBJL <= 8213))
                            {

                            }
                            else
                            {
                                // Invalid Object
                            }
                        }

                        if ((Feature_Builder.FRID.OBJL < 400) || (402 < Feature_Builder.FRID.OBJL))
                        {
                            this.Feature ??= new List<DCC.Feature>();
                            this.Feature.Add(Feature_Builder);
                        }
                    }


                    Category_Line = false;
                }
            }
        }


        #region [[ Read Element Method ]]
        protected void Read_Element(string tag, byte[] data, ref DCC.Vector? vector, ref DCC.Feature? feature)
        {
            switch (tag)
            {
                case "DSID": { Read_DSID(data); } break;
                case "DSSI": { Read_DSSI(data); } break;
                case "DSPM": { Read_DSPM(data); } break;
                case "VRID": { Read_VRID(data, ref vector); } break;
                case "VRPT": { Read_VRPT(data, ref vector); } break;
                case "VRPC": { Read_VRPC(data, ref vector); } break;
                case "SGCC": { Read_SGCC(data, ref vector); } break;
                case "SG2D": { Read_SG2D(data, ref vector); } break;
                case "SG3D": { Read_SG3D(data, ref vector); } break;
                case "ATTV": { Read_ATTV(data, ref vector); } break;
                case "FRID": { Read_FRID(data, ref feature); } break;
                case "FOID": { Read_FOID(data, ref feature); } break;
                case "FFPC": { Read_FFPC(data, ref feature); } break;
                case "FFPT": { Read_FFPT(data, ref feature); } break;
                case "FSPC": { Read_FSPC(data, ref feature); } break;
                case "FSPT": { Read_FSPT(data, ref feature); } break;
                case "ATTF": { Read_ATTF(data, ref feature); } break;
                case "NATF": { Read_NATF(data, ref feature); } break;
            }
        }

        protected unsafe void Read_DSID(byte[] data)
        {
            DCC.DSID DSID = new DCC.DSID();

            StringBuilder Text_Builder = new StringBuilder();
            int Index = 0;

            if (data.Length > Index) { DSID.RCNM = data[Index++]; }
            if (data.Length > (Index + 3)) {
                fixed (byte* RCID = &data[Index])
                {
                    DSID.RCID = *(uint*)RCID;
                }

                Index += 4;
            }
            if (data.Length > Index) { DSID.EXPP = data[Index++]; }
            if (data.Length > Index) { DSID.INTU = data[Index++]; }
            if (data.Length > Index) {
                while (Index < data.Length)
                {
                    byte Read = data[Index++];

                    if (Read == 0x1F) { break; }

                    Text_Builder.Append((char)Read);
                }

                DSID.DSNM = Text_Builder.ToString();

                Text_Builder.Clear();
            }
            if (data.Length > Index) {
                while (Index < data.Length)
                {
                    byte Read = data[Index++];

                    if (Read == 0x1F) { break; }

                    Text_Builder.Append((char)Read);
                }

                DSID.EDTN = Text_Builder.ToString();

                Text_Builder.Clear();
            }
            if (data.Length > Index) {
                while (Index < data.Length)
                {
                    byte Read = data[Index++];

                    if (Read == 0x1F) { break; }

                    Text_Builder.Append((char)Read);
                }

                DSID.UPDN = Text_Builder.ToString();

                Text_Builder.Clear();
            }
            if (data.Length > Index) {
                if ((Index + 7) < data.Length)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Text_Builder.Append((char)data[Index++]);
                    }

                    DSID.UPDT = Text_Builder.ToString();

                    Text_Builder.Clear();
                }
            }
            if (data.Length > Index) {
                if ((Index + 7) < data.Length)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Text_Builder.Append((char)data[Index++]);
                    }

                    DSID.ISDT = Text_Builder.ToString();

                    Text_Builder.Clear();
                }
            }
            if (data.Length > Index) {
                if ((Index + 3) < data.Length)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Text_Builder.Append((char)data[Index++]);
                    }

                    DSID.STED = Text_Builder.ToString();

                    Text_Builder.Clear();
                }
            }
            if (data.Length > Index) { DSID.PRSP = data[Index++]; }
            if (data.Length > Index) {
                while (Index < data.Length)
                {
                    byte Read = data[Index++];

                    if (Read == 0x1F) { break; }

                    Text_Builder.Append((char)Read);
                }

                DSID.PSDN = Text_Builder.ToString();

                Text_Builder.Clear();
            }
            if (data.Length > Index) {
                while (Index < data.Length)
                {
                    byte Read = data[Index++];

                    if (Read == 0x1F) { break; }

                    Text_Builder.Append((char)Read);
                }

                DSID.PRED = Text_Builder.ToString();

                Text_Builder.Clear();
            }
            if (data.Length > Index) { DSID.PROF = data[Index++]; }
            if (data.Length > (Index + 1)) {
                fixed (byte* AGEN = &data[Index])
                {
                    DSID.AGEN = *(ushort*)AGEN;
                }

                Index += 2;
            }
            if (data.Length > Index) {
                while (Index < data.Length)
                {
                    byte Read = data[Index++];

                    if (Read == 0x1F) { break; }

                    Text_Builder.Append((char)Read);
                }

                DSID.COMT = Text_Builder.ToString();

                Text_Builder.Clear();
            }

            this.DSID = DSID;


            if (int.TryParse(DSID.EDTN, out int EDTN))
            {
                this.Edition_Number = EDTN;
            }

            if (int.TryParse(DSID.UPDN, out int UPDN))
            {
                this.Update_Number = UPDN;
            }
        }

        protected unsafe void Read_DSSI(byte[] data)
        {
            DCC.DSSI DSSI = new DCC.DSSI();

            int Index = 0;

            if (data.Length > Index) { DSSI.DSTR = data[Index++]; }
            if (data.Length > Index) { DSSI.AALL = data[Index++]; }
            if (data.Length > Index) { DSSI.NALL = data[Index++]; }
            if (data.Length > (Index + 3)) {
                fixed (byte* NOMR = &data[Index])
                {
                    DSSI.NOMR = *(uint*)NOMR;
                }

                Index += 4;
            }
            if (data.Length > (Index + 3)) {
                fixed (byte* NOCR = &data[Index])
                {
                    DSSI.NOCR = *(uint*)NOCR;
                }

                Index += 4;
            }
            if (data.Length > (Index + 3)) {
                fixed (byte* NOGR = &data[Index])
                {
                    DSSI.NOGR = *(uint*)NOGR;
                }

                Index += 4;
            }
            if (data.Length > (Index + 3)) {
                fixed (byte* NOLR = &data[Index])
                {
                    DSSI.NOLR = *(uint*)NOLR;
                }

                Index += 4;
            }
            if (data.Length > (Index + 3)) {
                fixed (byte* NOIN = &data[Index])
                {
                    DSSI.NOIN = *(uint*)NOIN;
                }

                Index += 4;
            }
            if (data.Length > (Index + 3)) {
                fixed (byte* NOCN = &data[Index])
                {
                    DSSI.NOCN = *(uint*)NOCN;
                }

                Index += 4;
            }
            if (data.Length > (Index + 3)) {
                fixed (byte* NOED = &data[Index])
                {
                    DSSI.NOED = *(uint*)NOED;
                }

                Index += 4;
            }
            if (data.Length > (Index + 3)) {
                fixed (byte* NOFA = &data[Index])
                {
                    DSSI.NOFA = *(uint*)NOFA;
                }

                Index += 4;
            }

            this.DSSI = DSSI;
        }

        protected unsafe void Read_DSPM(byte[] data)
        {
            DCC.DSPM DSPM = new DCC.DSPM();

            int Index = 0;

            if (data.Length > Index) { DSPM.RCNM = data[Index++]; }
            if (data.Length > (Index + 3)) {
                fixed (byte* RCID = &data[Index])
                {
                    DSPM.RCID = *(uint*)RCID;
                }

                Index += 4;
            }
            if (data.Length > Index) { DSPM.HDAT = data[Index++]; }
            if (data.Length > Index) { DSPM.VDAT = data[Index++]; }
            if (data.Length > Index) { DSPM.SDAT = data[Index++]; }
            if (data.Length > (Index + 3)) {
                fixed (byte* CSCL = &data[Index])
                {
                    DSPM.CSCL = *(uint*)CSCL;
                }

                Index += 4;
            }
            if (data.Length > Index) { DSPM.DUNI = data[Index++]; }
            if (data.Length > Index) { DSPM.HUNI = data[Index++]; }
            if (data.Length > Index) { DSPM.PUNI = data[Index++]; }
            if (data.Length > Index) { DSPM.COUN = data[Index++]; }
            if (data.Length > (Index + 3)) {
                fixed (byte* COMF = &data[Index])
                {
                    DSPM.COMF = *(uint*)COMF;
                }

                Index += 4;
            }
            if (data.Length > (Index + 3)) {
                fixed (byte* SOMF = &data[Index])
                {
                    DSPM.SOMF = *(uint*)SOMF;
                }

                Index += 4;
            }
            if (data.Length > Index)
            {
                StringBuilder Text_Builder = new StringBuilder();

                while (Index < data.Length)
                {
                    byte Read = data[Index++];

                    if (Read == 0x1F) { break; }

                    Text_Builder.Append((char)Read);
                }

                DSPM.COMT = Text_Builder.ToString();
            }

            this.DSPM = DSPM;
        }

        protected unsafe void Read_VRID(byte[] data, ref DCC.Vector? vector)
        {
            if (data.Length > 0)
            {
                DCC.VRID VRID = new DCC.VRID();

                int Index = 0;

                if (data.Length > Index) { VRID.RCNM = data[Index++]; }
                if (data.Length > (Index + 3)) {
                    fixed (byte* RCID = &data[Index])
                    {
                        VRID.RCID = *(uint*)RCID;
                    }

                    Index += 4;
                }
                if (data.Length > (Index + 1)) {
                    fixed (byte* RVER = &data[Index])
                    {
                        VRID.RVER = *(ushort*)RVER;
                    }

                    Index += 2;
                }
                if (data.Length > Index) { VRID.RUIN = data[Index++]; }

                vector ??= new DCC.Vector();
                vector.VRID = VRID;
            }
        }

        protected unsafe void Read_VRPT(byte[] data, ref DCC.Vector? vector)
        {
            int Index = 0;

            while (Index < data.Length)
            {
                if (data.Length > (Index + 8))
                {
                    DCC.VRPT VRPT = new DCC.VRPT();
                    VRPT.KEY1 = data[Index];
                    VRPT.Name = VRPT.KEY1.ToString();

                    fixed (byte* KEY2 = &data[Index + 1])
                    {
                        VRPT.KEY2 = *(uint*)KEY2;
                        VRPT.Name += VRPT.KEY2.ToString();
                    }

                    VRPT.ORNT = data[Index + 5];
                    VRPT.USAG = data[Index + 6];
                    VRPT.TOPI = data[Index + 7];
                    VRPT.MASK = data[Index + 8];

                    vector ??= new DCC.Vector();
                    vector.VRPT ??= new List<DCC.VRPT>();
                    vector.VRPT.Add(VRPT);
                }

                Index += 9;
            }
        }

        protected unsafe void Read_VRPC(byte[] data, ref DCC.Vector? vector)
        {
            if (data.Length > 0)
            {
                DCC.VRPC VRPC = new DCC.VRPC();

                int Index = 0;

                if (data.Length > Index) { VRPC.VPUI = data[Index++]; }
                if (data.Length > (Index + 1)) {
                    fixed (byte* VPIX = &data[Index])
                    {
                        VRPC.VPIX = *(ushort*)VPIX;
                    }

                    Index += 2;
                }
                if (data.Length > (Index + 1)) {
                    fixed (byte* NVPT = &data[Index])
                    {
                        VRPC.NVPT = *(ushort*)NVPT;
                    }

                    Index += 2;
                }

                vector ??= new DCC.Vector();
                vector.VRPC ??= new List<DCC.VRPC>();
                vector.VRPC.Add(VRPC);
            }
        }

        protected unsafe void Read_SGCC(byte[] data, ref DCC.Vector? vector)
        {
            if (data.Length > 0)
            {
                DCC.SGCC SGCC = new DCC.SGCC();

                int Index = 0;

                if (data.Length > Index) { SGCC.CCUI = data[Index++]; }
                if (data.Length > (Index + 1)) {
                    fixed (byte* VPIX = &data[Index])
                    {
                        SGCC.CCIX = *(ushort*)VPIX;
                    }

                    Index += 2;
                }
                if (data.Length > (Index + 1)) {
                    fixed (byte* NVPT = &data[Index])
                    {
                        SGCC.CCNC = *(ushort*)NVPT;
                    }

                    Index += 2;
                }

                vector ??= new DCC.Vector();
                vector.SGCC ??= new List<DCC.SGCC>();
                vector.SGCC.Add(SGCC);
            }
        }

        protected unsafe void Read_SG2D(byte[] data, ref DCC.Vector? vector)
        {
            int Index = 0;

            while (Index < data.Length)
            {
                if (data.Length > (Index + 7))
                {
                    DCC.SG2D SG2D = new DCC.SG2D();

                    fixed (byte* YCOO = &data[Index])
                    {
                        SG2D.YCOO = *(int*)YCOO;
                    }

                    fixed (byte* XCOO = &data[Index + 4])
                    {
                        SG2D.XCOO = *(int*)XCOO;
                    }


                    if (this.DSPM.COMF != 0)
                    {
                        (double X, double Y) COMF = (
                            X: (double)SG2D.XCOO / this.DSPM.COMF,
                            Y: (double)SG2D.YCOO / this.DSPM.COMF
                        );

                        if (COMF.X >= 180.0)
                        {
                            COMF.X = 179.9999999;
                        }
                        else if (COMF.X <= -180.0)
                        {
                            COMF.X = -179.9999999;
                        }

                        // 참고 소스에서 Cell이 Bound true가 되는 경우가 없어서 완전히 생략했었는 듯?
                        //if ((Boundary.South <= COMF.Y) && (COMF.Y <= Boundary.North) && (Boundary.West <= COMF.X) && (COMF.X <= Boundary.East))
                        //{

                        //}
                        //else
                        //{

                        //}

                        SG2D.XCOO = (int)(COMF.X * 10000000.0);
                        SG2D.YCOO = (int)(COMF.Y * 10000000.0);
                    }

                    vector ??= new DCC.Vector();
                    vector.SG2D ??= new List<DCC.SG2D>();
                    vector.SG2D.Add(SG2D);
                }

                Index += 8;
            }
        }

        protected unsafe void Read_SG3D(byte[] data, ref DCC.Vector? vector)
        {
            int Index = 0;

            while (Index < data.Length)
            {
                if (data.Length > (Index + 11))
                {
                    DCC.SG3D SG3D = new DCC.SG3D();

                    fixed (byte* YCOO = &data[Index])
                    {
                        SG3D.YCOO = *(int*)YCOO;
                    }

                    fixed (byte* XCOO = &data[Index + 4])
                    {
                        SG3D.XCOO = *(int*)XCOO;
                    }

                    fixed (byte* VE3D = &data[Index + 8])
                    {
                        SG3D.VE3D = *(int*)VE3D;
                    }


                    if (this.DSPM.COMF != 0)
                    {
                        (double X, double Y) COMF = (
                            X: (double)SG3D.XCOO / this.DSPM.COMF,
                            Y: (double)SG3D.YCOO / this.DSPM.COMF
                        );

                        if (COMF.X >= 180.0)
                        {
                            COMF.X = 179.9999999;
                        }
                        else if (COMF.X <= -180.0)
                        {
                            COMF.X = -179.9999999;
                        }

                        // 참고 소스에서 Cell이 Bound true가 되는 경우가 없어서 완전히 생략했었는 듯?
                        //if ((Boundary.South <= COMF.Y) && (COMF.Y <= Boundary.North) && (Boundary.West <= COMF.X) && (COMF.X <= Boundary.East))
                        //{

                        //}
                        //else
                        //{

                        //}

                        SG3D.XCOO = (int)(COMF.X * 10000000.0);
                        SG3D.YCOO = (int)(COMF.Y * 10000000.0);
                    }

                    if (this.DSPM.SOMF != 0)
                    {
                        SG3D.VE3D = (int)(SG3D.VE3D * 10.0 / this.DSPM.SOMF);
                    }

                    vector ??= new DCC.Vector();
                    vector.SG3D ??= new List<DCC.SG3D>();
                    vector.SG3D.Add(SG3D);
                }

                Index += 12;
            }
        }

        protected unsafe void Read_ATTV(byte[] data, ref DCC.Vector? vector)
        {
            if (data.Length > 0)
            {
                DCC.ATTV ATTV = new DCC.ATTV();

                int Index = 0;

                if (data.Length > (Index + 1)) {
                    fixed (byte* ATTL = &data[Index])
                    {
                        ATTV.ATTL = *(ushort*)ATTL;
                    }

                    Index += 2;
                }
                if (data.Length > Index) { ATTV.ATVL = data[Index++].ToString(); }

                vector ??= new DCC.Vector();
                vector.ATTV ??= new List<DCC.ATTV>();
                vector.ATTV.Add(ATTV);
            }
        }

        protected unsafe void Read_FRID(byte[] data, ref DCC.Feature? feature)
        {
            if (data.Length > 0)
            {
                DCC.FRID FRID = new DCC.FRID();

                int Index = 0;

                if (data.Length > Index) { FRID.RCNM = data[Index++]; }
                if (data.Length > (Index + 3)) {
                    fixed (byte* RCID = &data[Index])
                    {
                        FRID.RCID = *(uint*)RCID;
                    }

                    Index += 4;
                }
                if (data.Length > Index) { FRID.PRIM = data[Index++]; }
                if (data.Length > Index) { FRID.GRUP = data[Index++]; }
                if (data.Length > (Index + 1)) {
                    fixed (byte* OBJL = &data[Index])
                    {
                        FRID.OBJL = *(ushort*)OBJL;
                    }

                    Index += 2;
                }
                if (data.Length > (Index + 1)) {
                    fixed (byte* RVER = &data[Index])
                    {
                        FRID.RVER = *(ushort*)RVER;
                    }

                    Index += 2;
                }
                if (data.Length > Index) { FRID.RUIN = data[Index++]; }

                feature ??= new DCC.Feature();
                feature.FRID = FRID;
            }
        }

        protected unsafe void Read_FOID(byte[] data, ref DCC.Feature? feature)
        {
            if (data.Length > 0)
            {
                DCC.FOID FOID = new DCC.FOID();

                int Index = 0;

                if (data.Length > (Index + 1)) {
                    fixed (byte* AGEN = &data[Index])
                    {
                        FOID.AGEN = *(ushort*)AGEN;
                    }

                    Index += 2;
                }
                if (data.Length > (Index + 3)) {
                    fixed (byte* AGEN = &data[Index])
                    {
                        FOID.FIDN = *(uint*)AGEN;
                    }

                    Index += 4;
                }
                if (data.Length > (Index + 1)) {
                    fixed (byte* FIDS = &data[Index])
                    {
                        FOID.FIDS = *(ushort*)FIDS;
                    }

                    Index += 2;
                }

                feature ??= new DCC.Feature();
                feature.FOID = FOID;
            }
        }

        protected unsafe void Read_FFPC(byte[] data, ref DCC.Feature? feature)
        {
            if (data.Length > 0)
            {
                DCC.FFPC FFPC = new DCC.FFPC();

                int Index = 0;

                if (data.Length > Index) { FFPC.FFUI = data[Index++]; }
                if (data.Length > (Index + 1)) {
                    fixed (byte* FFIX = &data[Index])
                    {
                        FFPC.FFIX = *(ushort*)FFIX;
                    }

                    Index += 2;
                }
                if (data.Length > (Index + 1)) {
                    fixed (byte* NOPT = &data[Index])
                    {
                        FFPC.NOPT = *(ushort*)NOPT;
                    }

                    Index += 2;
                }

                feature ??= new DCC.Feature();
                feature.FFPC ??= new List<DCC.FFPC>();
                feature.FFPC.Add(FFPC);
            }
        }

        protected unsafe void Read_FFPT(byte[] data, ref DCC.Feature? feature)
        {
            StringBuilder Text_Builder = new StringBuilder();
            int Index = 0;

            while (Index < data.Length)
            {
                DCC.FFPT FFPT = new DCC.FFPT();
                FFPT.RIND = data[Index++];

                while (Index < data.Length)
                {
                    byte Read = data[Index++];

                    if (Read == 0x1F) { break; }

                    Text_Builder.Append((char)Read);
                }

                FFPT.COMT = Text_Builder.ToString();

                Text_Builder.Clear();

                feature ??= new DCC.Feature();
                feature.FFPT ??= new List<DCC.FFPT>();
                feature.FFPT.Add(FFPT);
            }
        }

        protected unsafe void Read_FSPC(byte[] data, ref DCC.Feature? feature)
        {
            if (data.Length > 0)
            {
                DCC.FSPC FSPC = new DCC.FSPC();

                int Index = 0;

                if (data.Length > Index) { FSPC.FSUI = data[Index++]; }
                if (data.Length > (Index + 1)) {
                    fixed (byte* FSIX = &data[Index])
                    {
                        FSPC.FSIX = *(ushort*)FSIX;
                    }

                    Index += 2;
                }
                if (data.Length > (Index + 1)) {
                    fixed (byte* NSPT = &data[Index])
                    {
                        FSPC.NSPT = *(ushort*)NSPT;
                    }

                    Index += 2;
                }

                feature ??= new DCC.Feature();
                feature.FSPC ??= new List<DCC.FSPC>();
                feature.FSPC.Add(FSPC);
            }
        }

        protected unsafe void Read_FSPT(byte[] data, ref DCC.Feature? feature)
        {
            int Index = 0;

            while (Index < data.Length)
            {
                if (data.Length > (Index + 7))
                {
                    DCC.FSPT FSPT = new DCC.FSPT();
                    FSPT.KEY1 = data[Index];
                    FSPT.Name = FSPT.KEY1.ToString();

                    fixed (byte* KEY2 = &data[Index + 1])
                    {
                        FSPT.KEY2 = *(uint*)KEY2;
                        FSPT.Name += FSPT.KEY2.ToString();
                    }

                    FSPT.ORNT = data[Index + 5];
                    FSPT.USAG = data[Index + 6];
                    FSPT.MASK = data[Index + 7];

                    feature ??= new DCC.Feature();
                    feature.FSPT ??= new List<DCC.FSPT>();
                    feature.FSPT.Add(FSPT);

                }

                Index += 8;
            }
        }

        protected unsafe void Read_ATTF(byte[] data, ref DCC.Feature? feature)
        {
            StringBuilder Text_Builder = new StringBuilder();
            int Index = 0;

            while (Index < data.Length)
            {
                DCC.ATTF ATTF = new DCC.ATTF();

                if (data.Length > (Index + 1)) {
                    fixed (byte* ATTL = &data[Index])
                    {
                        ATTF.ATTL = *(ushort*)ATTL;
                    }

                    Index += 2;
                }

                while (Index < data.Length)
                {
                    byte Read = data[Index++];

                    if (Read == 0x1F) { break; }

                    Text_Builder.Append((char)Read);
                }

                string ATVL = Text_Builder.ToString();

                Text_Builder.Clear();

                if (!string.IsNullOrEmpty(ATVL))
                {
                    if ((ATTF.ATTL == 192) || (ATTF.ATTL == 148))
                    {
                        ATTF.ATVL = new string[] { ATVL.Replace("\"", "") };
                    }
                    else
                    {
                        string[] ATVL_Segment = ATVL.Split(',');

                        ATTF.ATVL = new string[ATVL_Segment.Length];

                        for (int i = 0; i < ATVL_Segment.Length; i++)
                        {
                            if (ATTF.ATTL == 4)
                            {
                                if (int.TryParse(ATVL_Segment[i], out int ATVL_Data) && (ATVL_Data > 8))
                                {

                                }
                            }

                            ATTF.ATVL[i] = string.IsNullOrEmpty(ATVL_Segment[i]) ? "9999" : ATVL_Segment[i];
                        }
                    }
                }
                else
                {
                    ATTF.ATVL = new string[] { "9999" };
                }

                feature ??= new DCC.Feature();
                feature.ATTF ??= new List<DCC.ATTF>();
                feature.ATTF.Add(ATTF);
            }
        }

        protected unsafe void Read_NATF(byte[] data, ref DCC.Feature? feature)
        {
            StringBuilder Text_Builder = new StringBuilder();
            int Index = 0;

            while (Index < data.Length)
            {
                DCC.NATF NATF = new DCC.NATF();

                if (data.Length > (Index + 1)) {
                    fixed (byte* ATTL = &data[Index])
                    {
                        NATF.ATTL = *(ushort*)ATTL;
                    }

                    Index += 2;
                }

                while (Index < data.Length)
                {
                    if (this.DSSI.NALL == 2)
                    {
                        if (data.Length > (Index + 1)) {
                            ushort Read;

                            fixed (byte* ATVL = &data[Index])
                            {
                                Read = *(ushort*)ATVL;
                            }

                            Index += 2;

                            if (Read == 0x001F) { break; }

                            Text_Builder.Append((char)Read);
                        }
                        else {
                            break;
                        }
                    }
                    else
                    {
                        byte Read = data[Index++];

                        if (Read == 0x1F) { break; }

                        Text_Builder.Append((char)Read);
                    }
                }

                NATF.ATVL = Text_Builder.ToString();

                Text_Builder.Clear();

                feature ??= new DCC.Feature();
                feature.NATF ??= new List<DCC.NATF>();
                feature.NATF.Add(NATF);
            }
        }
        #endregion


        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="cell_reader"></param>
        private void Read(StreamReader cell_reader)
        {
            char[] DSGI_Index = new char[5];

            if (cell_reader.ReadBlock(DSGI_Index, 0, 5) < 5) { return; }

            //Regex Header_Regex = new Regex(@"[0-9]{4};&.{2} .+");
            Regex Category_Regex = new Regex(@"[0-9]{5} D     [0-9]{5}   .+");
            StringBuilder Sentence_Builder = new StringBuilder();

            List<(string Tag_Name, int Data_Size)> Tag_List = new List<(string Tag_Name, int Data_Size)>();
            int Tag_Seeker = 0;

            DCC.Vector? Vector_Builder = null;
            DCC.Feature? Feature_Builder = null;

            int Read_Data = -1;
            bool Category_Line = false;

            while (true)
            {
                Read_Data = cell_reader.Read();

                if (Read_Data != -1)
                {
                    if (Read_Data == 0x001E)
                    {
                        string Data_Sentence = Sentence_Builder.ToString();

                        if (Category_Regex.Match(Data_Sentence).Success && (Data_Sentence.Length > 23))
                        {
                            Category_Line = true;

                            Tag_List.Clear();
                            Tag_Seeker = 0;

                            Vector_Builder = null;
                            Feature_Builder = null;

                            if (Data_Sentence.Length > 24)
                            {
                                string Tag_Line = Data_Sentence[24..];

                                int LF = Data_Sentence[20] - 48;
                                int PF = Data_Sentence[21] - 48;
                                int TF = Data_Sentence[23] - 48;

                                int Tag_Size = LF + PF + TF;
                                int Tag_Count = Tag_Line.Length / Tag_Size;

                                for (int i = 0; i < Tag_Count; i++)
                                {
                                    int Tag_Index = Tag_Size * i;
                                    string Tag_Name = Tag_Line.Substring(Tag_Index, TF);

                                    int Data_Size = 0;

                                    for (int j = 0; j < LF; j++)
                                    {
                                        int Digits = 1;

                                        for (int k = 0; k < j; k++)
                                        {
                                            Digits *= 10;
                                        }

                                        Data_Size += (Tag_Line[Tag_Index + Tag_Size - 1 - PF - j] - 48) * Digits;
                                    }

                                    Tag_List.Add((Tag_Name, Data_Size));
                                }
                            }

                            Sentence_Builder.Clear();
                        }
                        else
                        {
                            if (Category_Line)
                            {
                                if (Tag_List.Count <= Tag_Seeker)
                                {
                                    Category_Line = false;

                                    Sentence_Builder.Clear();
                                }
                                else
                                {
                                    (string Tag_Name, int Data_Size) Tag = Tag_List[Tag_Seeker];

                                    if ((Tag.Tag_Name == "NATF") && (this.DSSI.NALL == 2))
                                    {
                                        if (Data_Sentence.Length < (Tag.Data_Size - 1))
                                        {
                                            int Evader = cell_reader.Read();
                                            Tag.Data_Size -= 1;
                                        }
                                    }

                                    if (Data_Sentence.Length < (Tag.Data_Size - 1))
                                    {
                                        Sentence_Builder.Append((char)Read_Data);
                                    }
                                    else
                                    {
                                        Read_Element(Tag.Tag_Name, Data_Sentence, ref Vector_Builder, ref Feature_Builder);

                                        if (++Tag_Seeker == Tag_List.Count)
                                        {
                                            switch (Vector_Builder?.VRID.RCNM)
                                            {
                                                case 110:
                                                    {
                                                        this.VI ??= new List<DCC.Vector>();
                                                        this.VI.Add(Vector_Builder);
                                                    }
                                                    break;
                                                case 120:
                                                    {
                                                        this.VC ??= new List<DCC.Vector>();
                                                        this.VC.Add(Vector_Builder);
                                                    }
                                                    break;
                                                case 130:
                                                    {
                                                        this.VE ??= new List<DCC.Vector>();
                                                        this.VE.Add(Vector_Builder);
                                                    }
                                                    break;
                                            }

                                            if (Feature_Builder?.FRID.RCID > 0)
                                            {
                                                if (Feature_Builder.FRID.OBJL > 646)
                                                {
                                                    if ((8193 <= Feature_Builder.FRID.OBJL) && (Feature_Builder.FRID.OBJL <= 8213))
                                                    {

                                                    }
                                                    else
                                                    {
                                                        // Invalid Object
                                                    }
                                                }

                                                if ((Feature_Builder.FRID.OBJL < 400) || (402 < Feature_Builder.FRID.OBJL))
                                                {
                                                    this.Feature ??= new List<DCC.Feature>();
                                                    this.Feature.Add(Feature_Builder);
                                                }
                                            }
                                        }

                                        Sentence_Builder.Clear();
                                    }
                                }
                            }
                            else
                            {
                                Sentence_Builder.Clear();
                            }
                        }
                    }
                    else
                    {
                        Sentence_Builder.Append((char)Read_Data);
                    }
                }
                else
                {
                    break;
                }
            }
        }


        #region [[ Read Element Method ]]
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="data"></param>
        /// <param name="vector"></param>
        /// <param name="feature"></param>
        protected void Read_Element(string tag, string data, ref DCC.Vector? vector, ref DCC.Feature? feature)
        {
            switch (tag)
            {
                case "DSID": { Read_DSID(data); } break;
                case "DSSI": { Read_DSSI(data); } break;
                case "DSPM": { Read_DSPM(data); } break;
                case "VRID": { Read_VRID(data, ref vector); } break;
                case "VRPT": { Read_VRPT(data, ref vector); } break;
                case "VRPC": { Read_VRPC(data, ref vector); } break;
                case "SGCC": { Read_SGCC(data, ref vector); } break;
                case "SG2D": { Read_SG2D(data, ref vector); } break;
                case "SG3D": { Read_SG3D(data, ref vector); } break;
                case "ATTV": { Read_ATTV(data, ref vector); } break;
                case "FRID": { Read_FRID(data, ref feature); } break;
                case "FOID": { Read_FOID(data, ref feature); } break;
                case "FFPC": { Read_FFPC(data, ref feature); } break;
                case "FFPT": { Read_FFPT(data, ref feature); } break;
                case "FSPC": { Read_FSPC(data, ref feature); } break;
                case "FSPT": { Read_FSPT(data, ref feature); } break;
                case "ATTF": { Read_ATTF(data, ref feature); } break;
                case "NATF": { Read_NATF(data, ref feature); } break;
            }
        }

        protected unsafe void Read_DSID(string data)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    DCC.DSID DSID = new DCC.DSID();
            //    string[] Data_Segment = data.Split('\u001F');

            //    if (Data_Segment.Length > 0) {
            //        byte[] Byte_Segment = TextEncoding.RCP.GetBytes(Data_Segment[0]);

            //        if (Byte_Segment.Length > 0) { DSID.RCNM = Byte_Segment[0]; }
            //        if (Byte_Segment.Length > 1) {
            //            fixed (byte* RCID_Pointer = &Byte_Segment[1])
            //            {
            //                DSID.RCID = *(uint*)RCID_Pointer;
            //            }
            //        }
            //        if (Byte_Segment.Length > 5) { DSID.EXPP = Byte_Segment[5]; }
            //        if (Byte_Segment.Length > 6) { DSID.INTU = Byte_Segment[6]; }
            //        if (Byte_Segment.Length > 7) { DSID.DSNM = TextEncoding.RCP.GetString(Byte_Segment[7..]); }
            //    }
            //    if (Data_Segment.Length > 1) { DSID.EDTN = Data_Segment[1]; }
            //    if (Data_Segment.Length > 2) { DSID.UPDN = Data_Segment[2]; }
            //    if (Data_Segment.Length > 3) {
            //        if (Data_Segment[3].Length >= 8) { DSID.UPDT = Data_Segment[3][..8]; }
            //        if (Data_Segment[3].Length >= 16) { DSID.ISDT = Data_Segment[3][8..16]; }
            //        if (Data_Segment[3].Length >= 20) { DSID.STED = Data_Segment[3][16..20]; }
            //        if (Data_Segment[3].Length > 20) { DSID.PRSP = Data_Segment[3][20]; }
            //        if (Data_Segment[3].Length > 21) { DSID.PSDN = Data_Segment[3][21..]; }
            //    }
            //    if (Data_Segment.Length > 4) { DSID.PRED = Data_Segment[4]; }
            //    if (Data_Segment.Length > 5) {
            //        byte[] Byte_Segment = TextEncoding.RCP.GetBytes(Data_Segment[5]);

            //        if (Byte_Segment.Length > 0) { DSID.PROF = Byte_Segment[0]; }
            //        if (Byte_Segment.Length > 1) {
            //            fixed (byte* AGEN_Pointer = &Byte_Segment[1])
            //            {
            //                DSID.AGEN = *(ushort*)AGEN_Pointer;
            //            }
            //        }
            //        if (Byte_Segment.Length > 3) { DSID.COMT = TextEncoding.RCP.GetString(Byte_Segment[3..]); }
            //    }

            //    this.DSID = DSID;

            //    if (int.TryParse(DSID.EDTN, out int EDTN))
            //    {
            //        this.Edition_Number = EDTN;
            //    }

            //    if (int.TryParse(DSID.UPDN, out int UPDN))
            //    {
            //        this.Update_Number = UPDN;
            //    }
            //}
        }

        protected unsafe void Read_DSSI(string data)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    DCC.DSSI DSSI = new DCC.DSSI();
            //    byte[] Byte_Segment = TextEncoding.RCP.GetBytes(data);

            //    if (Byte_Segment.Length > 0) { DSSI.DSTR = Byte_Segment[0]; }
            //    if (Byte_Segment.Length > 1) { DSSI.AALL = Byte_Segment[1]; }
            //    if (Byte_Segment.Length > 2) { DSSI.NALL = Byte_Segment[2]; }
            //    if (Byte_Segment.Length > 3) {
            //        fixed (byte* NOMR_Pointer = &Byte_Segment[3])
            //        {
            //            DSSI.NOMR = *(uint*)NOMR_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 7) {
            //        fixed (byte* NOCR_Pointer = &Byte_Segment[7])
            //        {
            //            DSSI.NOCR = *(uint*)NOCR_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 11) {
            //        fixed (byte* NOGR_Pointer = &Byte_Segment[11])
            //        {
            //            DSSI.NOGR = *(uint*)NOGR_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 15) {
            //        fixed (byte* NOLR_Pointer = &Byte_Segment[15])
            //        {
            //            DSSI.NOLR = *(uint*)NOLR_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 19) {
            //        fixed (byte* NOIN_Pointer = &Byte_Segment[19])
            //        {
            //            DSSI.NOIN = *(uint*)NOIN_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 23) {
            //        fixed (byte* NOCN_Pointer = &Byte_Segment[23])
            //        {
            //            DSSI.NOCN = *(uint*)NOCN_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 27) {
            //        fixed (byte* NOED_Pointer = &Byte_Segment[27])
            //        {
            //            DSSI.NOED = *(uint*)NOED_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 31) {
            //        fixed (byte* NOFA_Pointer = &Byte_Segment[31])
            //        {
            //            DSSI.NOFA = *(uint*)NOFA_Pointer;
            //        }
            //    }

            //    this.DSSI = DSSI;
            //}
        }

        protected unsafe void Read_DSPM(string data)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    DCC.DSPM DSPM = new DCC.DSPM();
            //    byte[] Byte_Segment = TextEncoding.RCP.GetBytes(data);

            //    if (Byte_Segment.Length > 0) { DSPM.RCNM = Byte_Segment[0]; }
            //    if (Byte_Segment.Length > 1) {
            //        fixed (byte* RCID_Pointer = &Byte_Segment[1])
            //        {
            //            DSPM.RCID = *(uint*)RCID_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 5) { DSPM.HDAT = Byte_Segment[5]; }
            //    if (Byte_Segment.Length > 6) { DSPM.VDAT = Byte_Segment[6]; }
            //    if (Byte_Segment.Length > 7) { DSPM.SDAT = Byte_Segment[7]; }
            //    if (Byte_Segment.Length > 8) {
            //        fixed (byte* CSCL_Pointer = &Byte_Segment[8])
            //        {
            //            DSPM.CSCL = *(uint*)CSCL_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 12) { DSPM.DUNI = Byte_Segment[12]; }
            //    if (Byte_Segment.Length > 13) { DSPM.HUNI = Byte_Segment[13]; }
            //    if (Byte_Segment.Length > 14) { DSPM.PUNI = Byte_Segment[14]; }
            //    if (Byte_Segment.Length > 15) { DSPM.COUN = Byte_Segment[15]; }
            //    if (Byte_Segment.Length > 16) {
            //        fixed (byte* COMF_Pointer = &Byte_Segment[16])
            //        {
            //            DSPM.COMF = *(uint*)COMF_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 20) {
            //        fixed (byte* SOMF_Pointer = &Byte_Segment[20])
            //        {
            //            DSPM.SOMF = *(uint*)SOMF_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 24) { DSPM.COMT = TextEncoding.RCP.GetString(Byte_Segment[24..]).Split('\u001F')[0]; }

            //    this.DSPM = DSPM;
            //}
        }

        protected unsafe void Read_VRID(string data, ref DCC.Vector? vector)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    DCC.VRID VRID = new DCC.VRID();
            //    byte[] Byte_Segment = TextEncoding.RCP.GetBytes(data);

            //    if (Byte_Segment.Length > 0) { VRID.RCNM = Byte_Segment[0]; }
            //    if (Byte_Segment.Length > 1) {
            //        fixed (byte* RCID_Pointer = &Byte_Segment[1])
            //        {
            //            VRID.RCID = *(uint*)RCID_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 5) {
            //        fixed (byte* RVER_Pointer = &Byte_Segment[5])
            //        {
            //            VRID.RVER = *(ushort*)RVER_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 7) { VRID.RUIN = Byte_Segment[7]; }

            //    vector ??= new DCC.Vector();
            //    vector.VRID = VRID;
            //}
        }

        protected unsafe void Read_VRPT(string data, ref DCC.Vector? vector)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    byte[] Byte_Segment = TextEncoding.RCP.GetBytes(data);

            //    int Index = 0;

            //    while (Index < Byte_Segment.Length)
            //    {
            //        if ((Index + 9) <= Byte_Segment.Length)
            //        {
            //            DCC.VRPT VRPT = new DCC.VRPT();
            //            VRPT.KEY1 = Byte_Segment[Index];
            //            VRPT.Name = VRPT.KEY1.ToString();

            //            fixed (byte* KEY2_Pointer = &Byte_Segment[Index + 1])
            //            {
            //                VRPT.KEY2 = *(uint*)KEY2_Pointer;
            //                VRPT.Name += VRPT.KEY2.ToString();
            //            }

            //            VRPT.ORNT = Byte_Segment[Index + 5];
            //            VRPT.USAG = Byte_Segment[Index + 6];
            //            VRPT.TOPI = Byte_Segment[Index + 7];
            //            VRPT.MASK = Byte_Segment[Index + 8];

            //            vector ??= new DCC.Vector();
            //            vector.VRPT ??= new List<DCC.VRPT>();
            //            vector.VRPT.Add(VRPT);
            //        }

            //        Index += 9;
            //    }
            //}
        }

        protected unsafe void Read_VRPC(string data, ref DCC.Vector? vector)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    DCC.VRPC VRPC = new DCC.VRPC();
            //    byte[] Byte_Segment = TextEncoding.RCP.GetBytes(data);

            //    if (Byte_Segment.Length > 0) { VRPC.VPUI = Byte_Segment[0]; }
            //    if (Byte_Segment.Length > 1) {
            //        fixed (byte* VPIX_Pointer = &Byte_Segment[1])
            //        {
            //            VRPC.VPIX = *(ushort*)VPIX_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 3) {
            //        fixed (byte* NVPT_Pointer = &Byte_Segment[3])
            //        {
            //            VRPC.NVPT = *(ushort*)NVPT_Pointer;
            //        }
            //    }

            //    vector ??= new DCC.Vector();
            //    vector.VRPC ??= new List<DCC.VRPC>();
            //    vector.VRPC.Add(VRPC);
            //}
        }

        protected unsafe void Read_SGCC(string data, ref DCC.Vector? vector)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    DCC.SGCC SGCC = new DCC.SGCC();
            //    byte[] Byte_Segment = TextEncoding.RCP.GetBytes(data);

            //    if (Byte_Segment.Length > 0) { SGCC.CCUI = Byte_Segment[0]; }
            //    if (Byte_Segment.Length > 1) {
            //        fixed (byte* CCIX_Pointer = &Byte_Segment[1])
            //        {
            //            SGCC.CCIX = *(ushort*)CCIX_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 3) {
            //        fixed (byte* CCNC_Pointer = &Byte_Segment[3])
            //        {
            //            SGCC.CCNC = *(ushort*)CCNC_Pointer;
            //        }
            //    }

            //    vector ??= new DCC.Vector();
            //    vector.SGCC ??= new List<DCC.SGCC>();
            //    vector.SGCC.Add(SGCC);
            //}
        }

        protected unsafe void Read_SG2D(string data, ref DCC.Vector? vector)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    byte[] Byte_Segment = TextEncoding.RCP.GetBytes(data);

            //    int Index = 0;

            //    while (Index < Byte_Segment.Length)
            //    {
            //        if ((Index + 8) <= Byte_Segment.Length)
            //        {
            //            DCC.SG2D SG2D = new DCC.SG2D();

            //            fixed (byte* YCOO_Pointer = &Byte_Segment[Index])
            //            {
            //                SG2D.YCOO = *(int*)YCOO_Pointer;
            //            }

            //            fixed (byte* XCOO_Pointer = &Byte_Segment[Index + 4])
            //            {
            //                SG2D.XCOO = *(int*)XCOO_Pointer;
            //            }

            //            if (this.DSPM.COMF != 0)
            //            {
            //                (double X, double Y) COMF = (
            //                    X: (double)SG2D.XCOO / this.DSPM.COMF,
            //                    Y: (double)SG2D.YCOO / this.DSPM.COMF
            //                );

            //                if (COMF.X >= 180.0)
            //                {
            //                    COMF.X = 179.9999999;
            //                }
            //                else if (COMF.X <= -180.0)
            //                {
            //                    COMF.X = -179.9999999;
            //                }

            //                // 참고 소스에서 Cell이 Bound true가 되는 경우가 없어서 완전히 생략했었는 듯?
            //                //if ((Boundary.South <= COMF.Y) && (COMF.Y <= Boundary.North) && (Boundary.West <= COMF.X) && (COMF.X <= Boundary.East))
            //                //{

            //                //}
            //                //else
            //                //{

            //                //}

            //                SG2D.XCOO = (int)(COMF.X * 10000000.0);
            //                SG2D.YCOO = (int)(COMF.Y * 10000000.0);
            //            }

            //            vector ??= new DCC.Vector();
            //            vector.SG2D ??= new List<DCC.SG2D>();
            //            vector.SG2D.Add(SG2D);
            //        }

            //        Index += 8;
            //    }
            //}
        }

        protected unsafe void Read_SG3D(string data, ref DCC.Vector? vector)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    byte[] Byte_Segment = TextEncoding.RCP.GetBytes(data);

            //    int Index = 0;

            //    while (Index < Byte_Segment.Length)
            //    {
            //        if ((Index + 12) <= Byte_Segment.Length)
            //        {
            //            DCC.SG3D SG3D = new DCC.SG3D();

            //            fixed (byte* YCOO_Pointer = &Byte_Segment[Index])
            //            {
            //                SG3D.YCOO = *(int*)YCOO_Pointer;
            //            }

            //            fixed (byte* XCOO_Pointer = &Byte_Segment[Index + 4])
            //            {
            //                SG3D.XCOO = *(int*)XCOO_Pointer;
            //            }

            //            fixed (byte* VE3D_Pointer = &Byte_Segment[Index + 8])
            //            {
            //                SG3D.VE3D = *(int*)VE3D_Pointer;
            //            }

            //            if (this.DSPM.COMF != 0)
            //            {
            //                (double X, double Y) COMF = (
            //                    X: (double)SG3D.XCOO / this.DSPM.COMF,
            //                    Y: (double)SG3D.YCOO / this.DSPM.COMF
            //                );

            //                if (COMF.X >= 180.0)
            //                {
            //                    COMF.X = 179.9999999;
            //                }
            //                else if (COMF.X <= -180.0)
            //                {
            //                    COMF.X = -179.9999999;
            //                }

            //                // 참고 소스에서 Cell이 Bound true가 되는 경우가 없어서 완전히 생략했었는 듯?
            //                //if ((Boundary.South <= COMF.Y) && (COMF.Y <= Boundary.North) && (Boundary.West <= COMF.X) && (COMF.X <= Boundary.East))
            //                //{

            //                //}
            //                //else
            //                //{

            //                //}

            //                SG3D.XCOO = (int)(COMF.X * 10000000.0);
            //                SG3D.YCOO = (int)(COMF.Y * 10000000.0);
            //            }

            //            if (this.DSPM.SOMF != 0)
            //            {
            //                SG3D.VE3D = (int)(SG3D.VE3D * 10.0 / this.DSPM.SOMF);
            //            }

            //            vector ??= new DCC.Vector();
            //            vector.SG3D ??= new List<DCC.SG3D>();
            //            vector.SG3D.Add(SG3D);
            //        }

            //        Index += 12;
            //    }
            //}
        }

        protected unsafe void Read_ATTV(string data, ref DCC.Vector? vector)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    DCC.ATTV ATTV = new DCC.ATTV();
            //    byte[] Byte_Segment = TextEncoding.RCP.GetBytes(data);

            //    if (data.Length > 0) {
            //        fixed (byte* ATTL_Pointer = &Byte_Segment[0])
            //        {
            //            ATTV.ATTL = *(ushort*)ATTL_Pointer;
            //        }
            //    }
            //    if (data.Length > 2) { ATTV.ATVL = data[2].ToString(); }

            //    vector ??= new DCC.Vector();
            //    vector.ATTV ??= new List<DCC.ATTV>();
            //    vector.ATTV.Add(ATTV);
            //}
        }

        protected unsafe void Read_FRID(string data, ref DCC.Feature? feature)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    DCC.FRID FRID = new DCC.FRID();
            //    byte[] Byte_Segment = TextEncoding.RCP.GetBytes(data);

            //    if (Byte_Segment.Length > 0) { FRID.RCNM = Byte_Segment[0]; }
            //    if (Byte_Segment.Length > 1) {
            //        fixed (byte* RCID_Pointer = &Byte_Segment[1])
            //        {
            //            FRID.RCID = *(uint*)RCID_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 5) { FRID.PRIM = Byte_Segment[5]; }
            //    if (Byte_Segment.Length > 6) { FRID.GRUP = Byte_Segment[6]; }
            //    if (Byte_Segment.Length > 7) {
            //        fixed (byte* OBJL_Pointer = &Byte_Segment[7])
            //        {
            //            FRID.OBJL = *(ushort*)OBJL_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 9) {
            //        fixed (byte* RVER_Pointer = &Byte_Segment[9])
            //        {
            //            FRID.RVER = *(ushort*)RVER_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 11) { FRID.RUIN = Byte_Segment[11]; }

            //    feature ??= new DCC.Feature();
            //    feature.FRID = FRID;
            //}
        }

        protected unsafe void Read_FOID(string data, ref DCC.Feature? feature)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    DCC.FOID FOID = new DCC.FOID();
            //    byte[] Byte_Segment = TextEncoding.RCP.GetBytes(data);

            //    if (Byte_Segment.Length > 0) {
            //        fixed (byte* AGEN_Pointer = &Byte_Segment[0])
            //        {
            //            FOID.AGEN = *(ushort*)AGEN_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 2) {
            //        fixed (byte* FIDN_Pointer = &Byte_Segment[2])
            //        {
            //            FOID.FIDN = *(uint*)FIDN_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 6) {
            //        fixed (byte* FIDS_Pointer = &Byte_Segment[6])
            //        {
            //            FOID.FIDS = *(ushort*)FIDS_Pointer;
            //        }
            //    }

            //    feature ??= new DCC.Feature();
            //    feature.FOID = FOID;
            //}
        }

        protected unsafe void Read_FFPC(string data, ref DCC.Feature? feature)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    DCC.FFPC FFPC = new DCC.FFPC();
            //    byte[] Byte_Segment = TextEncoding.RCP.GetBytes(data);

            //    if (Byte_Segment.Length > 0) { FFPC.FFUI = Byte_Segment[0]; }
            //    if (Byte_Segment.Length > 1) {
            //        fixed (byte* FFIX_Pointer = &Byte_Segment[1])
            //        {
            //            FFPC.FFIX = *(ushort*)FFIX_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 3) {
            //        fixed (byte* NOPT_Pointer = &Byte_Segment[3])
            //        {
            //            FFPC.NOPT = *(ushort*)NOPT_Pointer;
            //        }
            //    }

            //    feature ??= new DCC.Feature();
            //    feature.FFPC ??= new List<DCC.FFPC>();
            //    feature.FFPC.Add(FFPC);
            //}
        }

        protected unsafe void Read_FFPT(string data, ref DCC.Feature? feature)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    string[] Data_Segment = data.Split('\u001F');

            //    foreach (string Data_Unit in Data_Segment)
            //    {
            //        if (!string.IsNullOrEmpty(Data_Unit))
            //        {
            //            DCC.FFPT FFPT = new DCC.FFPT();

            //            if (Data_Unit.Length > 8) { FFPT.RIND = Data_Unit[8]; }
            //            if (Data_Unit.Length > 9) { FFPT.COMT = Data_Unit[9..]; }

            //            feature ??= new DCC.Feature();
            //            feature.FFPT ??= new List<DCC.FFPT>();
            //            feature.FFPT.Add(FFPT);
            //        }
            //    }
            //}
        }

        protected unsafe void Read_FSPC(string data, ref DCC.Feature? feature)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    DCC.FSPC FSPC = new DCC.FSPC();
            //    byte[] Byte_Segment = TextEncoding.RCP.GetBytes(data);

            //    if (Byte_Segment.Length > 0) { FSPC.FSUI = Byte_Segment[0]; }
            //    if (Byte_Segment.Length > 1) {
            //        fixed (byte* FSIX_Pointer = &Byte_Segment[1])
            //        {
            //            FSPC.FSIX = *(ushort*)FSIX_Pointer;
            //        }
            //    }
            //    if (Byte_Segment.Length > 3) {
            //        fixed (byte* NSPT_Pointer = &Byte_Segment[3])
            //        {
            //            FSPC.NSPT = *(ushort*)NSPT_Pointer;
            //        }
            //    }

            //    feature ??= new DCC.Feature();
            //    feature.FSPC ??= new List<DCC.FSPC>();
            //    feature.FSPC.Add(FSPC);
            //}
        }

        protected unsafe void Read_FSPT(string data, ref DCC.Feature? feature)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    byte[] Byte_Segment = TextEncoding.RCP.GetBytes(data);

            //    int Index = 0;

            //    while (Index < Byte_Segment.Length)
            //    {
            //        if ((Index + 8) <= Byte_Segment.Length)
            //        {
            //            DCC.FSPT FSPT = new DCC.FSPT();
            //            FSPT.KEY1 = Byte_Segment[Index];
            //            FSPT.Name = FSPT.KEY1.ToString();

            //            fixed (byte* KEY2_Pointer = &Byte_Segment[Index + 1])
            //            {
            //                FSPT.KEY2 = *(uint*)KEY2_Pointer;
            //                FSPT.Name += FSPT.KEY2.ToString();
            //            }

            //            FSPT.ORNT = Byte_Segment[Index + 5];
            //            FSPT.USAG = Byte_Segment[Index + 6];
            //            FSPT.MASK = Byte_Segment[Index + 7];

            //            feature ??= new DCC.Feature();
            //            feature.FSPT ??= new List<DCC.FSPT>();
            //            feature.FSPT.Add(FSPT);
            //        }

            //        Index += 8;
            //    }
            //}
        }

        protected unsafe void Read_ATTF(string data, ref DCC.Feature? feature)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    string[] Data_Segment = data.Split('\u001F');

            //    foreach (string Data_Unit in Data_Segment)
            //    {
            //        if (!string.IsNullOrEmpty(Data_Unit))
            //        {
            //            DCC.ATTF ATTF = new DCC.ATTF();
            //            byte[] Byte_Segment = TextEncoding.RCP.GetBytes(Data_Unit);

            //            if (Byte_Segment.Length > 0) {
            //                fixed (byte* ATTL_Pointer = &Byte_Segment[0])
            //                {
            //                    ATTF.ATTL = *(ushort*)ATTL_Pointer;
            //                }
            //            }
            //            if (Data_Unit.Length > 2) {
            //                if ((ATTF.ATTL == 192) || (ATTF.ATTL == 148))
            //                {
            //                    ATTF.ATVL = new string[] { Data_Unit.Substring(2).Replace("\"", "") };
            //                }
            //                else
            //                {
            //                    string[] ATVL_Segment = Data_Unit.Substring(2).Split(',');
            //                    ATTF.ATVL = new string[ATVL_Segment.Length];

            //                    for (int i = 0; i < ATVL_Segment.Length; i++)
            //                    {
            //                        if (ATTF.ATTL == 4)
            //                        {
            //                            if (int.TryParse(ATVL_Segment[i], out int ATVL_Data) && (ATVL_Data > 8))
            //                            {

            //                            }
            //                        }

            //                        ATTF.ATVL[i] = string.IsNullOrEmpty(ATVL_Segment[i]) ? "9999" : ATVL_Segment[i];
            //                    }
            //                }
            //            }
            //            else {
            //                ATTF.ATVL = new string[] { "9999" };
            //            }

            //            feature ??= new DCC.Feature();
            //            feature.ATTF ??= new List<DCC.ATTF>();
            //            feature.ATTF.Add(ATTF);
            //        }
            //    }
            //}
        }

        protected unsafe void Read_NATF(string data, ref DCC.Feature? feature)
        {
            //if (!string.IsNullOrEmpty(data))
            //{
            //    string[] Data_Segment = (this.DSSI.NALL == 2) ? data.Split("\u001F\0") : data.Split('\u001F');

            //    foreach (string Data_Unit in Data_Segment)
            //    {
            //        if (!string.IsNullOrEmpty(Data_Unit))
            //        {
            //            DCC.NATF NATF = new DCC.NATF() { NALL = this.DSSI.NALL };
            //            byte[] Byte_Segment = TextEncoding.RCP.GetBytes(Data_Unit);

            //            if (Byte_Segment.Length > 0) {
            //                fixed (byte* ATTL_Pointer = &Byte_Segment[0])
            //                {
            //                    NATF.ATTL = *(ushort*)ATTL_Pointer;
            //                }
            //            }

            //            if (NATF.NALL == 2)
            //            {
            //                if (Byte_Segment.Length > 2) {
            //                    NATF.ATVL = Encoding.Unicode.GetString(Byte_Segment[2..]);
            //                }
            //            }
            //            else
            //            {
            //                if (data.Length > 2) {
            //                    NATF.ATVL = data[2..];
            //                }
            //            }

            //            feature ??= new DCC.Feature();
            //            feature.NATF ??= new List<DCC.NATF>();
            //            feature.NATF.Add(NATF);
            //        }
            //    }
            //}
        }
        #endregion
    }
}