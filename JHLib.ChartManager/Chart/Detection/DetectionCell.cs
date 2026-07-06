using JHLib.ChartManager.ENC.Chart;
using JHLib.ChartManager.ENC.Chart.Feature;
using JHLib.ChartManager.ENC.Chart.Vector;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace JHLib.ChartManager.Chart.Detection
{
    public class DetectionCell
    {
        public DSID DSID { get; private set; }
        public DSSI DSSI { get; private set; }
        public DSPM DSPM { get; private set; }

        public List<DetectionCell.Vector> VI { get; private set; } = new List<DetectionCell.Vector>(); // Isolate Node
        public List<DetectionCell.Vector> VC { get; private set; } = new List<DetectionCell.Vector>(); // Connected Node
        public List<DetectionCell.Vector> VE { get; private set; } = new List<DetectionCell.Vector>(); // Edge

        public List<DetectionCell.Feature> feature { get; private set; } = new List<Feature>();

        public (double north, double south, double east, double west)? boundary = null;

        public int? editionNumber { get; protected set; } = null;
        public int? updateNumber { get; protected set; } = null;

        public bool absorbed { get; internal set; } = false;



        public DetectionCell()
        {
            this.DSID = new DSID();
            this.DSSI = new DSSI();
            this.DSPM = new DSPM();
        }

        public DetectionCell(DSPM DSPM)
        {
            this.DSID = new DSID();
            this.DSSI = new DSSI();
            this.DSPM = DSPM;
        }



        public void Read(string filePath)
        {
            using (BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open)))
            {
                Read(reader);
            }
        }

        public void Read(Stream fileStream)
        {
            using (BinaryReader reader = new BinaryReader(fileStream))
            {
                Read(reader);
            }
        }

        private void Read(BinaryReader reader)
        {
            if (reader.BaseStream.Length < 5) { return; }

            byte[] DSGI = new byte[5] {
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
            };

            //Regex headerRegex = new Regex(@"[0-9]{4};&.{2} .+");
            Regex categoryRegex = new Regex(@"[0-9]{5} D     [0-9]{5}   .+");

            List<(string tag, byte[] data)> elementRecord = new List<(string tag, byte[] data)>();

            DetectionCell.Vector? vectorBuilder = null;
            DetectionCell.Feature? featureBuilder = null;

            bool readingCategoryLine = false;
            byte read = 0;

            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                if (!readingCategoryLine)
                {
                    StringBuilder categoryBuilder = new StringBuilder();

                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        read = reader.ReadByte();

                        if (read == 0x1E) { break; }

                        categoryBuilder.Append((char)read);
                    }

                    string categorySentence = categoryBuilder.ToString();

                    if (categoryRegex.Match(categorySentence).Success)
                    {
                        readingCategoryLine = true;

                        elementRecord.Clear();

                        vectorBuilder = null;
                        featureBuilder = null;

                        if (categorySentence.Length > 24)
                        {
                            string elementLine = categorySentence[24..];

                            int LF = categorySentence[20] - 48;
                            int PF = categorySentence[21] - 48;
                            int TF = categorySentence[23] - 48;

                            int elementSize = LF + PF + TF;
                            int elementCount = elementLine.Length / elementSize;

                            for (int i = 0; i < elementCount; i++)
                            {
                                int tagIndex = elementSize * i;
                                string tagName = elementLine.Substring(tagIndex, TF);

                                int dataSize = 0;

                                for (int j = 0; j < LF; j++)
                                {
                                    int digits = 1;

                                    for (int k = 0; k < j; k++)
                                    {
                                        digits *= 10;
                                    }

                                    dataSize += (elementLine[tagIndex + elementSize - 1 - PF - j] - 48) * digits;
                                }

                                elementRecord.Add((tagName, new byte[dataSize]));
                            }
                        }
                    }
                }
                else
                {
                    foreach ((string tag, byte[] data) element in elementRecord)
                    {
                        for (int i = 0; i < element.data.Length; i++)
                        {
                            if (reader.BaseStream.Position != reader.BaseStream.Length)
                            {
                                element.data[i] = reader.ReadByte();
                            }
                            else
                            {
                                break;
                            }
                        }

                        if ((this.DSSI.NALL == 2) &&
                            (element.tag == "NATF") &&
                            (element.data[^2] == 0x1E) &&
                            (element.data[^1] == 0x00))
                        {
                            ReadElement(element.tag, element.data, ref vectorBuilder, ref featureBuilder);
                        }
                        else if (element.data[^1] == 0x1E)
                        {
                            ReadElement(element.tag, element.data, ref vectorBuilder, ref featureBuilder);
                        }
                    }

                    switch (vectorBuilder?.VRID.RCNM)
                    {
                        case 110:
                            {
                                this.VI.Add(vectorBuilder);
                            }
                            break;
                        case 120:
                            {
                                this.VC.Add(vectorBuilder);
                            }
                            break;
                        case 130:
                            {
                                this.VE.Add(vectorBuilder);
                            }
                            break;
                    }

                    if (featureBuilder?.FRID.RCID > 0)
                    {
                        if (featureBuilder.FRID.OBJL > 646)
                        {
                            if ((8193 <= featureBuilder.FRID.OBJL) && (featureBuilder.FRID.OBJL <= 8213))
                            {

                            }
                            else
                            {
                                // Invalid Object
                            }
                        }

                        if ((featureBuilder.FRID.OBJL < 400) || (402 < featureBuilder.FRID.OBJL))
                        {
                            this.feature.Add(featureBuilder);
                        }
                    }

                    readingCategoryLine = false;
                }
            }
        }

        protected void ReadElement(string tag, byte[] data, ref DetectionCell.Vector? vector, ref DetectionCell.Feature? feature)
        {
            switch (tag)
            {
                case "DSID": { ReadDSID(data); } break;
            }
        }

        #region [[ Sub Methods of ReadElement ]]
        protected unsafe void ReadDSID(byte[] data)
        {
            DSID DSID = new DSID();
            StringBuilder builder = new StringBuilder();

            int index = 0;

            if (data.Length > index) { DSID.RCNM = data[index++]; }
            if (data.Length > (index + 3)) {
                fixed (byte* RCID = &data[index])
                {
                    DSID.RCID = *(uint*)RCID;
                }

                index += 4;
            }
            if (data.Length > index) { DSID.EXPP = data[index++]; }
            if (data.Length > index) { DSID.INTU = data[index++]; }
            if (data.Length > index) {
                while (index < data.Length)
                {
                    byte read = data[index++];

                    if (read == 0x1F) { break; }

                    builder.Append((char)read);
                }

                DSID.DSNM = builder.ToString();

                builder.Clear();
            }
            if (data.Length > index) {
                while (index < data.Length)
                {
                    byte read = data[index++];

                    if (read == 0x1F) { break; }

                    builder.Append((char)read);
                }

                DSID.EDTN = builder.ToString();

                builder.Clear();
            }
            if (data.Length > index) {
                while (index < data.Length)
                {
                    byte read = data[index++];

                    if (read == 0x1F) { break; }

                    builder.Append((char)read);
                }

                DSID.UPDN = builder.ToString();

                builder.Clear();
            }

            //

            this.DSID = DSID;

            if (int.TryParse(DSID.EDTN, out int EDTN))
            {
                this.editionNumber = EDTN;
            }

            if (int.TryParse(DSID.UPDN, out int UPDN))
            {
                this.updateNumber = UPDN;
            }
        }
        #endregion



        public class Vector
        {
            public VRID VRID { get; private set; }




        }

        public class Feature
        {
            public FRID FRID { get; private set; }




        }
    }
}