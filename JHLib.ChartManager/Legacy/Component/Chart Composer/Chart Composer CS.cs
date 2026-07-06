using Legacy.ECM_Core.Chart;
using Legacy.ECM_Core.Table;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Legacy.ECM_Core.Component
{
    public partial class ChartComposer
    {
        private int Select_CS(DetectionChart chart, DCC.Feature feature, DCC.FeatureLinker linker, Dictionary<uint, DCC.UndGroup> und_group, ENC.CS cs, DCC.DrawCommand draw_command)
        {
            int Result = 0;

            switch (cs.Acronym)
            {
                case string DEPARE when DEPARE.Contains("DEPARE"): { Select_DEPARE03(chart, linker); } break;
                case string DRGARE when DRGARE.Contains("DRGARE"): { Select_DEPARE03(chart, linker); } break;
                case string SLCONS when SLCONS.Contains("SLCONS"): { Select_SLCONS04(feature, linker, draw_command); } break;
                case string TOPMAR when TOPMAR.Contains("TOPMAR"): { Select_TOPMAR02(chart, feature, linker, draw_command); } break;
                case string DEPCNT when DEPCNT.Contains("DEPCNT"): if (linker.Group_Layer == 19) { Select_DEPCNT03(feature, linker); } break;
                case string LIGHT when LIGHT.Contains("LIGHT"): { Select_LIGHTS06(chart, feature, linker); } break;
                case string OBSTRN when OBSTRN.Contains("OBSTRN"): { Select_OBSTRN07(feature, linker, und_group, draw_command); } break;
                case string WRECKS when WRECKS.Contains("WRECKS"): { Select_WRECKS05(feature, linker, und_group, draw_command); } break;
                case string SOUNDG when SOUNDG.Contains("SOUNDG"): { Select_SOUNDG03(feature, linker); } break;
                case string RESARE when RESARE.Contains("RESARE"): { Result = Select_RESARE04(feature, linker, draw_command); } break;
                case string QUAPOS when QUAPOS.Contains("QUAPOS"): { Select_QUAPOS01(feature, linker, draw_command); } break;
                case string SYMINS when SYMINS.Contains("SYMINS"): { Select_SYMINS02(feature, linker, draw_command); } break;
                case string RESTRN when RESTRN.Contains("RESTRN"): { Select_RESTRN01(feature, linker, draw_command); } break;
                case string CLRLIN when CLRLIN.Contains("CLRLIN"): { Select_CLRLIN01(feature, linker, draw_command); } break;
                case string DATCVR when DATCVR.Contains("DATCVR"): { Select_DATCVR02(feature, linker, draw_command); } break;
            }

            return Result;
        }

        private void Select_DEPARE03(DetectionChart chart, DCC.FeatureLinker linker)
        {
            if (linker.Shape == null) { return; }


            foreach (DCC.ShapeLinker Shape_Linker in linker.Shape)
            {
                if (Shape_Linker.Edge == null) { continue; }

                foreach (DCC.EdgeLinker Edge_Linker in Shape_Linker.Edge)
                {
                    if (chart.Get_Vector(Edge_Linker.RCNM, Edge_Linker.RCID, out DCC.Vector Vector))
                    {
                        DCC.EdgeAttribute Edge_Attribute = new DCC.EdgeAttribute()
                        {
                            UNSAFE = false,
                            VALDCO = float.MaxValue,
                            DRVAL1 = float.MaxValue,
                        };

                        if (Vector.Linked_Feature != null)
                        {
                            foreach (DCC.Feature Feature in Vector.Linked_Feature)
                            {
                                if (Feature.FRID.OBJL == 43)
                                {
                                    if (Feature.Get_ATVL(174, out string ATVL) && (ATVL != "9999"))
                                    {
                                        float.TryParse(ATVL, out Edge_Attribute.VALDCO);
                                    }
                                    else
                                    {
                                        Edge_Attribute.VALDCO = 0.0f;
                                    }
                                }


                                bool Share = false;

                                if (((Feature.FRID.PRIM == 3) && (Feature.FRID.OBJL == 42)) || (Feature.FRID.OBJL == 46))
                                {
                                    if (Feature.FRID.RCID != linker.FRID.RCID)
                                    {
                                        Share = true;

                                        if (Feature.Get_ATVL(87, out string ATVL) && (ATVL != "9999"))
                                        {
                                            float.TryParse(ATVL, out Edge_Attribute.DRVAL1);
                                        }
                                        else
                                        {
                                            Edge_Attribute.DRVAL1 = -1.0f;
                                        }
                                    }
                                }

                                if (!Share)
                                {
                                    if ((Feature.FRID.PRIM == 3) && ((Feature.FRID.OBJL == 71) || (Feature.FRID.OBJL == 154)))
                                    {
                                        IEnumerable<DCC.Feature> InlandFeature_Enumeration = Vector.Linked_Feature.Where(Feature => (Feature.FRID.OBJL == 23) || (Feature.FRID.OBJL == 45) || (Feature.FRID.OBJL == 69) || (Feature.FRID.OBJL == 114) || (Feature.FRID.OBJL == 79));

                                        if (InlandFeature_Enumeration.Count() < 1)
                                        {
                                            bool Linear = false;
                                            IEnumerable<DCC.Feature> LinearFeature_Enumeration = Vector.Linked_Feature.Where(Feature => (Feature.FRID.OBJL == 71) || (Feature.FRID.OBJL == 61) || (Feature.FRID.OBJL == 38));

                                            if (LinearFeature_Enumeration.Count() > 0)
                                            {
                                                Linear = true;
                                            }
                                            else
                                            {
                                                if (Feature.ATTF != null)
                                                {
                                                    IEnumerable<DCC.Feature> Feature_Enumeration = Vector.Linked_Feature.Where(Feature => (Feature.FRID.OBJL == 26) || (Feature.FRID.OBJL == 122));

                                                    if (Feature_Enumeration.Count() > 0)
                                                    {
                                                        IEnumerable<DCC.ATTF> ATTF_Enumeration = Feature.ATTF.Where(ATTF => {
                                                            if (ATTF.ATTL == 187)
                                                            {
                                                                if ((ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] != "9999"))
                                                                {
                                                                    return (byte.TryParse(ATTF.ATVL[0], out byte WATLEV) && ((WATLEV == 1) || (WATLEV == 2) || (WATLEV == 6)));
                                                                }
                                                                else
                                                                {
                                                                    return true;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                return false;
                                                            }
                                                        });

                                                        if (ATTF_Enumeration.Count() > 0)
                                                        {
                                                            Linear = true;
                                                        }
                                                    }
                                                }
                                            }

                                            if (!Linear) { Edge_Attribute.UNSAFE = true; }
                                        }
                                    }
                                }
                            }
                        }

                        Edge_Linker.Edge_Attribute = Edge_Attribute;
                    }
                }
            }
        }

        private void Select_SLCONS04(DCC.Feature feature, DCC.FeatureLinker linker, DCC.DrawCommand draw_command)
        {
            if (linker.FRID.PRIM == 1)
            {
                if (linker.Shape?.Count > 0)
                {
                    uint QUAPOS = (linker.FRID.OBJL == 129) ? linker.Shape[0].Vector_3D.ATVL : linker.Shape[0].Vector_2D.ATVL;

                    if (QUAPOS == 0)
                    {
                        draw_command.SY ??= new List<DCC.SY>();
                        draw_command.SY.Add(new DCC.SY() {
                            Index = SymbolTable.Table.TryGetValue("LOWACC01", out int Symbol_Index) ? Symbol_Index : -1,
                        });
                    }
                }
            }
            else
            {
                feature.Get_ATVL(81, out string CONDTN);
                feature.Get_ATVL(60, out string CATSLC);
                feature.Get_ATVL(187, out string WATLEV);

                DCC.LS LS = new DCC.LS()
                {
                    Pen_Type = 0,
                    Pen_Width = 2,
                    Pen_ColorIndex = ColorTable.Table.TryGetValue("CSTLN", out int Color_Index) ? Color_Index : -1,
                };

                if ((CONDTN == "1") || (CONDTN == "2"))
                {
                    LS.Pen_Type = 1;
                    LS.Pen_Width = 1;
                }
                else if ((CATSLC == "6") || (CATSLC == "15") || (CATSLC == "16"))
                {
                    LS.Pen_Width = 4;
                }
                else if (WATLEV == "2")
                {
                    LS.Pen_Width = 2;
                }
                else if ((WATLEV == "3") || (WATLEV == "4"))
                {
                    LS.Pen_Type = 1;
                    LS.Pen_Width = 2;
                }

                draw_command.LS ??= new List<DCC.LS>();
                draw_command.LS.Add(LS);

                if (linker.Shape != null)
                {
                    foreach (DCC.ShapeLinker Shape_Linker in linker.Shape)
                    {
                        if (Shape_Linker.Edge != null)
                        {
                            foreach (DCC.EdgeLinker Edge_Linker in Shape_Linker.Edge)
                            {
                                if (Edge_Linker.ATVL == 0)
                                {
                                    Edge_Linker.Edge_Command.LC = LineTable.Table.TryGetValue("LOWACC21", out int Line_Index) ? Line_Index : -1;
                                }
                                else
                                {
                                    Edge_Linker.Edge_Command.LS = LS;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void Select_TOPMAR02(DetectionChart chart, DCC.Feature feature, DCC.FeatureLinker linker, DCC.DrawCommand draw_command)
        {
            if (feature.Get_ATVL(171, out string ATVL) && (ATVL != "9999"))
            {
                if ((linker.FRID.PRIM == 1) && (linker.Shape?.Count > 0))
                {
                    byte RCNM;
                    uint RCID;

                    if (linker.FRID.OBJL == 129)
                    {
                        RCNM = linker.Shape[0].Vector_3D.RCNM;
                        RCID = linker.Shape[0].Vector_3D.RCID;
                    }
                    else
                    {
                        RCNM = linker.Shape[0].Vector_2D.RCNM;
                        RCID = linker.Shape[0].Vector_2D.RCID;
                    }

                    if (chart.Get_Vector(RCNM, RCID, out DCC.Vector Vector) && int.TryParse(ATVL, out int TOPSHP))
                    {
                        bool Floating = false;

                        if (Vector.Linked_Feature != null)
                        {
                            foreach (DCC.Feature Linked_Feature in Vector.Linked_Feature)
                            {
                                if (Linked_Feature.FRID.OBJL == 84)
                                {
                                    if(Linked_Feature.Get_ATVL(40, out string LinkATVL) && (LinkATVL != "9999") && int.TryParse(LinkATVL, out int CATMOR))
                                    {
                                        if(CATMOR == 7)
                                        {
                                            Floating = true;
                                            break;
                                        }
                                    }
                                }
                                else if (((14 <= Linked_Feature.FRID.OBJL) && (Linked_Feature.FRID.OBJL < 20)) || (Linked_Feature.FRID.OBJL == 76) || (Linked_Feature.FRID.OBJL == 77))
                                {
                                    Floating = true;
                                    break;
                                }
                                else if ((5 <= Linked_Feature.FRID.OBJL) && (Linked_Feature.FRID.OBJL < 10))
                                {
                                    Floating = false;
                                    break;
                                }
                            }
                        }

                        draw_command.SY ??= new List<DCC.SY>();
                        draw_command.SY.Add(new DCC.SY() {
                            Index = TOPSHP switch {
                                1 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR02" : "TOPMAR22", out int Symbol_Index) ? Symbol_Index : -1,
                                2 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR04" : "TOPMAR24", out int Symbol_Index) ? Symbol_Index : -1,
                                3 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR10" : "TOPMAR30", out int Symbol_Index) ? Symbol_Index : -1,
                                4 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR12" : "TOPMAR32", out int Symbol_Index) ? Symbol_Index : -1,
                                5 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR13" : "TOPMAR33", out int Symbol_Index) ? Symbol_Index : -1,
                                6 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR14" : "TOPMAR34", out int Symbol_Index) ? Symbol_Index : -1,
                                7 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR65" : "TOPMAR85", out int Symbol_Index) ? Symbol_Index : -1,
                                8 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR17" : "TOPMAR86", out int Symbol_Index) ? Symbol_Index : -1,
                                9 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR16" : "TOPMAR36", out int Symbol_Index) ? Symbol_Index : -1,
                                10 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR08" : "TOPMAR28", out int Symbol_Index) ? Symbol_Index : -1,
                                11 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR07" : "TOPMAR27", out int Symbol_Index) ? Symbol_Index : -1,
                                12 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR14" : "TOPMAR14", out int Symbol_Index) ? Symbol_Index : -1,
                                13 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR05" : "TOPMAR25", out int Symbol_Index) ? Symbol_Index : -1,
                                14 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR06" : "TOPMAR26", out int Symbol_Index) ? Symbol_Index : -1,
                                15 => SymbolTable.Table.TryGetValue(Floating ? "TMARDEF2" : "TOPMAR88", out int Symbol_Index) ? Symbol_Index : -1,
                                16 => SymbolTable.Table.TryGetValue(Floating ? "TMARDEF2" : "TOPMAR87", out int Symbol_Index) ? Symbol_Index : -1,
                                17 => SymbolTable.Table.TryGetValue(Floating ? "TMARDEF2" : "TMARDEF1", out int Symbol_Index) ? Symbol_Index : -1,
                                18 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR10" : "TOPMAR30", out int Symbol_Index) ? Symbol_Index : -1,
                                19 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR13" : "TOPMAR33", out int Symbol_Index) ? Symbol_Index : -1,
                                20 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR14" : "TOPMAR34", out int Symbol_Index) ? Symbol_Index : -1,
                                21 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR13" : "TOPMAR33", out int Symbol_Index) ? Symbol_Index : -1,
                                22 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR14" : "TOPMAR34", out int Symbol_Index) ? Symbol_Index : -1,
                                23 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR14" : "TOPMAR34", out int Symbol_Index) ? Symbol_Index : -1,
                                24 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR02" : "TOPMAR22", out int Symbol_Index) ? Symbol_Index : -1,
                                25 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR04" : "TOPMAR24", out int Symbol_Index) ? Symbol_Index : -1,
                                26 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR10" : "TOPMAR30", out int Symbol_Index) ? Symbol_Index : -1,
                                27 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR17" : "TOPMAR86", out int Symbol_Index) ? Symbol_Index : -1,
                                28 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR18" : "TOPMAR89", out int Symbol_Index) ? Symbol_Index : -1,
                                29 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR02" : "TOPMAR22", out int Symbol_Index) ? Symbol_Index : -1,
                                30 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR17" : "TOPMAR86", out int Symbol_Index) ? Symbol_Index : -1,
                                31 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR14" : "TOPMAR14", out int Symbol_Index) ? Symbol_Index : -1,
                                32 => SymbolTable.Table.TryGetValue(Floating ? "TOPMAR10" : "TOPMAR30", out int Symbol_Index) ? Symbol_Index : -1,
                                33 => SymbolTable.Table.TryGetValue(Floating ? "TMARDEF2" : "TMARDEF1", out int Symbol_Index) ? Symbol_Index : -1,
                                _ => SymbolTable.Table.TryGetValue(Floating ? "TMARDEF2" : "TMARDEF1", out int Symbol_Index) ? Symbol_Index : -1,
                            },
                            Angle = 0.0f,
                        });
                    }
                }
            }
            else
            {
                draw_command.SY ??= new List<DCC.SY>();
                draw_command.SY.Add(new DCC.SY() {
                    Index = SymbolTable.Table.TryGetValue("QUESMRK1", out int Symbol_Index) ? Symbol_Index : -1,
                    Angle = 0.0f,
                });
            }
        }

        private void Select_DEPCNT03(DCC.Feature feature, DCC.FeatureLinker linker)
        {
            if (linker.Shape == null) { return; }


            float VALDCO;

            if (feature.Get_ATVL(174, out string ATVL) && (ATVL != "9999"))
            {
                VALDCO = float.TryParse(ATVL, out float Value) ? Value : 0.0f;
            }
            else
            {
                VALDCO = 0.0f;
            }

            foreach (DCC.ShapeLinker Shape_Linker in linker.Shape)
            {
                if (Shape_Linker.Edge != null)
                {
                    foreach (DCC.EdgeLinker Edge_Liner in Shape_Linker.Edge)
                    {
                        Edge_Liner.Edge_Attribute.VALDCO = VALDCO;
                    }
                }
            }
        }

        private void Select_LIGHTS06(DetectionChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            bool[] CATLIT = new bool[21];
            bool[] STATUS = new bool[19];
            bool[] COLOUR = new bool[14];
            bool[] LITVIS = new bool[9];

            bool CATLIT_Exist = false;
            bool STATUS_Exist = false;
            bool COLOUR_Exist = false;
            bool LITVIS_Exist = false;

            float HEIGHT = float.MaxValue;
            byte LITCHR = 255;
            float ORIENT = float.MaxValue;
            float SECTR1 = float.MaxValue;
            float SECTR2 = float.MaxValue;
            float SIGPER = float.MaxValue;
            float VALNMR = float.MaxValue;
            string SIGGRP = "";

            if (feature.ATTF != null)
            {
                foreach (DCC.ATTF ATTF in feature.ATTF)
                {
                    switch (ATTF.ATTL)
                    {
                        case 95:
                            if ((ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] != "9999"))
                            {
                                float.TryParse(ATTF.ATVL[0], out HEIGHT);
                            }
                            break;
                        case 107:
                            if ((ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] != "9999"))
                            {
                                byte.TryParse(ATTF.ATVL[0], out LITCHR);
                            }
                            break;
                        case 117:
                            if ((ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] != "9999"))
                            {
                                float.TryParse(ATTF.ATVL[0], out ORIENT);
                            }
                            break;
                        case 136:
                            if ((ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] != "9999"))
                            {
                                float.TryParse(ATTF.ATVL[0], out SECTR1);
                            }
                            break;
                        case 137:
                            if ((ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] != "9999"))
                            {
                                float.TryParse(ATTF.ATVL[0], out SECTR2);
                            }
                            break;
                        case 142:
                            if ((ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] != "9999"))
                            {
                                float.TryParse(ATTF.ATVL[0], out SIGPER);
                            }
                            break;
                        case 178:
                            if ((ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] != "9999"))
                            {
                                float.TryParse(ATTF.ATVL[0], out VALNMR);
                            }
                            break;
                        case 141:
                            if ((ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] != "9999"))
                            {
                                SIGGRP = ATTF.ATVL[0];
                            }
                            break;
                        case 37:
                            {
                                foreach (string ATVL in ATTF.ATVL)
                                {
                                    if ((ATVL != "9999") && int.TryParse(ATVL, out int CATLIT_Index) && (0 <= CATLIT_Index) && (CATLIT_Index < CATLIT.Length))
                                    {
                                        CATLIT[CATLIT_Index] = true;
                                    }
                                }

                                CATLIT_Exist = true;
                            }
                            break;
                        case 75:
                            {
                                foreach (string ATVL in ATTF.ATVL)
                                {
                                    if ((ATVL != "9999") && int.TryParse(ATVL, out int COLOUR_Index) && (0 <= COLOUR_Index) && (COLOUR_Index < COLOUR.Length))
                                    {
                                        COLOUR[COLOUR_Index] = true;
                                    }
                                }

                                COLOUR_Exist = true;
                            }
                            break;
                        case 108:
                            {
                                foreach (string ATVL in ATTF.ATVL)
                                {
                                    if ((ATVL != "9999") && int.TryParse(ATVL, out int LITVIS_Index) && (0 <= LITVIS_Index) && (LITVIS_Index < LITVIS.Length))
                                    {
                                        LITVIS[LITVIS_Index] = true;
                                    }
                                }

                                LITVIS_Exist = true;
                            }
                            break;
                        case 149:
                            {
                                foreach (string ATVL in ATTF.ATVL)
                                {
                                    if ((ATVL != "9999") && int.TryParse(ATVL, out int STATUS_Index) && (0 <= STATUS_Index) && (STATUS_Index < STATUS.Length))
                                    {
                                        STATUS[STATUS_Index] = true;
                                    }
                                }

                                STATUS_Exist = true;
                            }
                            break;
                    }
                }
            }

            string LITDSN = Select_LITDSN02(CATLIT, COLOUR, STATUS, CATLIT_Exist, COLOUR_Exist, STATUS_Exist, LITCHR, SIGGRP, SIGPER, HEIGHT, VALNMR);

            bool CATLIT_8_11 = false;
            bool CATLIT_9 = false;
            bool CATLIT_1_16 = false;
            bool LITVIS_3_7_8 = false;

            if (VALNMR == float.MaxValue) { VALNMR = 9.0f; }

            if (CATLIT_Exist)
            {
                if (CATLIT[8] || CATLIT[11]) { CATLIT_8_11 = true; }
                if (CATLIT[9]) { CATLIT_9 = true; }
                if (CATLIT[1] || CATLIT[16]) { CATLIT_1_16 = true; }
            }

            if (LITVIS_Exist)
            {
                if (LITVIS[3] || LITVIS[7] || LITVIS[8]) { LITVIS_3_7_8 = true; }
            }

            byte Colour = 0;
            bool All_Round_Light = false;
            bool Flare_At_45_Degrees = false;
            bool Extended_Arc_Radius = false;
            bool Radius_26mm = false;

            if (!CATLIT_Exist || (!CATLIT_8_11 && !CATLIT_9))
            {
                if (!COLOUR_Exist) { Colour = 12; }
                if ((SECTR1 == float.MaxValue) || (SECTR2 == float.MaxValue)) { All_Round_Light = true; }

                if (!All_Round_Light)
                {
                    float Difference = Math.Abs(SECTR2 - SECTR1);

                    if (Difference > 360.0f) { Difference -= 360.0f; }
                    if ((Difference <= 0.0001f) || (Difference == 360.0f)) { All_Round_Light = true; }
                }

                if (All_Round_Light)
                {
                    if ((VALNMR >= 10.0f) && !(CATLIT[5] || CATLIT[6]) && (LITCHR != 12))
                    {
                        Radius_26mm = true; // Draw a 360 degree arc with radius 26mm

                        Colour = COLOUR switch {
                            _ when (COLOUR[1] && COLOUR[3]) => 1, // LITRD
                            _ when COLOUR[3] => 1,
                            _ when (COLOUR[1] && COLOUR[4]) => 2, // LITGN
                            _ when COLOUR[4] => 2,
                            _ when COLOUR[11] => 3, // LITYW
                            _ when (COLOUR[5] && COLOUR[6]) => 3,
                            _ when COLOUR[6] => 3,
                            _ when COLOUR[1] => 3,
                            _ => 0,
                        };
                    }
                    else
                    {
                        if ((linker.Shape?.Count > 0) && chart.Get_Vector(linker.Shape[0].Vector_2D.RCNM, linker.Shape[0].Vector_2D.RCID, out DCC.Vector Vector))
                        {
                            bool Flare = false;

                            if (Vector.Linked_Feature != null)
                            {
                                foreach (DCC.Feature Linked_Feature in Vector.Linked_Feature)
                                {
                                    if ((Linked_Feature.FRID.OBJL == 75) && (Linked_Feature.FRID.RCID != linker.FRID.RCID))
                                    {
                                        string ATVL;
                                        bool Sectr1 = Linked_Feature.Get_ATVL(136, out ATVL);
                                        bool Sectr2 = Linked_Feature.Get_ATVL(137, out ATVL);

                                        if (!Sectr1 || !Sectr2)
                                        {
                                            Flare = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            
                            if (Flare)
                            {
                                if (COLOUR[1] || COLOUR[6] || COLOUR[11])
                                {
                                    Flare_At_45_Degrees = true;
                                }
                            }
                            else
                            {
                                Colour = COLOUR switch {
                                    _ when (COLOUR[1] && COLOUR[3]) => 1, // LIGHTS11
                                    _ when COLOUR[3] => 1,
                                    _ when (COLOUR[1] && COLOUR[4]) => 2, // LIGHTS12
                                    _ when COLOUR[4] => 2,
                                    _ when COLOUR[11] => 3, // LIGHTS13
                                    _ when (COLOUR[5] && COLOUR[6]) => 3,
                                    _ when COLOUR[6] => 3,
                                    _ when COLOUR[1] => 3,
                                    _ => 0,
                                };
                            }
                        }
                    }
                }
                else // Continuation
                {
                    if (SECTR2 <= SECTR1) { SECTR2 += 360.0f; }

                    if ((linker.Shape?.Count > 0) && chart.Get_Vector(linker.Shape[0].Vector_2D.RCNM, linker.Shape[0].Vector_2D.RCID, out DCC.Vector Vector))
                    {
                        if (Vector.Linked_Feature != null)
                        {
                            foreach (DCC.Feature Linked_Feature in Vector.Linked_Feature)
                            {
                                if ((Linked_Feature.FRID.OBJL == 75) && (Linked_Feature.FRID.RCID != linker.FRID.RCID))
                                {
                                    Linked_Feature.Get_ATVL(136, out string ATVL1);
                                    Linked_Feature.Get_ATVL(137, out string ATVL2);

                                    float.TryParse(ATVL1, out float Sectr1);
                                    float.TryParse(ATVL2, out float Sectr2);

                                    if (((Sectr1 <= SECTR1) && (Sectr2 >= SECTR2)) || ((Sectr1 >= SECTR1) && (Sectr2 <= SECTR2)))
                                    {
                                        if ((Sectr2 - Sectr1) > (SECTR2 - SECTR1))
                                        {
                                            Extended_Arc_Radius = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (!LITVIS_3_7_8)
                    {
                        Colour = COLOUR switch {
                            _ when (COLOUR[1] && COLOUR[3]) => 1, // LITRD
                            _ when COLOUR[3] => 1,
                            _ when (COLOUR[1] && COLOUR[4]) => 2, // LITGN
                            _ when COLOUR[4] => 2,
                            _ when COLOUR[11] => 3, // LITYW
                            _ when (COLOUR[5] && COLOUR[6]) => 3,
                            _ when COLOUR[6] => 3,
                            _ when COLOUR[1] => 3,
                            _ => 0,
                        };
                    }
                }
            }
            
            linker.LITDSN = LITDSN;
            linker.ORIENT = ORIENT;
            linker.VALNMR = VALNMR;
            linker.SECTR1 = SECTR1;
            linker.SECTR2 = SECTR2;
            linker.All_Round_Light = All_Round_Light;
            linker.Extended_Arc_Radius = Extended_Arc_Radius;
            linker.CATLIT_1_16 = CATLIT_1_16;
            linker.CATLIT_8_11 = CATLIT_8_11;
            linker.CATLIT_9 = CATLIT_9;
            linker.LITVIS_3_7_8 = LITVIS_3_7_8;
            linker.COLOUR = Colour;
            linker.Flare_At_45_Degrees = Flare_At_45_Degrees;
            linker.Radius_26mm = Radius_26mm;
        }

        private void Select_OBSTRN07(DCC.Feature feature, DCC.FeatureLinker linker, Dictionary<uint, DCC.UndGroup> und_group, DCC.DrawCommand draw_command)
        {
            bool QUASOU = false;
            bool STATUS = false;
            bool TECSOU = false;

            float VALSOU = float.MaxValue;
            byte WATLEV = 255;
            byte EXPSOU = 255;
            byte CATOBS = 255;

            if (feature.ATTF != null)
            {
                foreach (DCC.ATTF ATTF in feature.ATTF)
                {
                    if (ATTF.ATVL.Length > 0)
                    {
                        switch (ATTF.ATTL)
                        {
                            case 125:
                                {
                                    IEnumerable<string> ATVL_Enumeration = ATTF.ATVL.Where(ATVL => int.TryParse(ATVL, out int QUASOU) && ((QUASOU == 3) || (QUASOU == 4) || (QUASOU == 5) || (QUASOU == 8) || (QUASOU == 9)));

                                    if (ATVL_Enumeration.Count() > 0)
                                    {
                                        QUASOU = true;
                                    }
                                }
                                break;
                            case 149:
                                {
                                    IEnumerable<string> ATVL_Enumeration = ATTF.ATVL.Where(ATVL => int.TryParse(ATVL, out int STATUS) && (STATUS == 18));

                                    if (ATVL_Enumeration.Count() > 0)
                                    {
                                        STATUS = true;
                                    }
                                }
                                break;
                            case 156:
                                {
                                    IEnumerable<string> ATVL_Enumeration = ATTF.ATVL.Where(ATVL => int.TryParse(ATVL, out int TECSOU) && ((TECSOU == 4) || (TECSOU == 6)));

                                    if (ATVL_Enumeration.Count() > 0)
                                    {
                                        TECSOU = true;
                                    }
                                }
                                break;
                            case 179:
                                {
                                    if (ATTF.ATVL[0] != "9999")
                                    {
                                        float.TryParse(ATTF.ATVL[0], out VALSOU);
                                    }
                                }
                                break;
                            case 93:
                                {
                                    if (ATTF.ATVL[0] != "9999")
                                    {
                                        byte.TryParse(ATTF.ATVL[0], out EXPSOU);
                                    }
                                }
                                break;
                            case 187:
                                {
                                    if (ATTF.ATVL[0] != "9999")
                                    {
                                        byte.TryParse(ATTF.ATVL[0], out WATLEV);
                                    }
                                }
                                break;
                            case 42:
                                {
                                    if (ATTF.ATVL[0] != "9999")
                                    {
                                        byte.TryParse(ATTF.ATVL[0], out CATOBS);
                                    }
                                }
                                break;
                        }
                    }
                }
            }

            (float LEAST, float SEABED, float MAX) DEPTH = Select_DEPVAL02(linker, und_group, WATLEV, EXPSOU);
            linker.DRVAL1 = DEPTH.MAX;

            if (VALSOU != float.MaxValue)
            {
                linker.Danger_DEPTH = VALSOU;
            }
            else
            {
                if (DEPTH.LEAST == float.MaxValue)
                {
                    if ((CATOBS == 6) || (WATLEV == 3))
                    {
                        linker.Danger_DEPTH = 0.01f;
                    }
                    else if (WATLEV == 5)
                    {
                        linker.Danger_DEPTH = 0.0f;
                    }
                    else
                    {
                        linker.Danger_DEPTH = -15.0f;
                    }
                }
                else
                {
                    linker.Danger_DEPTH = DEPTH.LEAST;
                }
            }

            linker.VALSOU = VALSOU;
            linker.Danger_WATLEV_1_2 = ((WATLEV == 1) || (WATLEV == 2));
            linker.Danger_Accuracy = !Select_QUAPNT02(linker);

            switch (linker.FRID.PRIM)
            {
                case 1:
                    {
                        string Symbol;

                        if (VALSOU == float.MaxValue)
                        {
                            if (linker.FRID.OBJL == 153)
                            {
                                Symbol = (WATLEV == 3) ? "UWTROC03" : "UWTROC04";
                            }
                            else
                            {
                                if (CATOBS == 6)
                                {
                                    Symbol = "OBSTRN01";
                                }
                                else
                                {
                                    if ((WATLEV == 1) || (WATLEV == 2))
                                    {
                                        Symbol = "OBSTRN11";
                                    }
                                    else if ((WATLEV == 4) || (WATLEV == 5))
                                    {
                                        Symbol = "OBSTRN03";
                                    }
                                    else
                                    {
                                        Symbol = "OBSTRN01";
                                    }
                                }
                            }
                        }
                        else
                        {
                            bool Sounding = false;

                            if (linker.FRID.OBJL == 153)
                            {
                                if ((WATLEV == 4) || (WATLEV == 5))
                                {
                                    Symbol = "UWTROC04";
                                }
                                else
                                {
                                    Symbol = "DANGER01";
                                    Sounding = true;
                                }
                            }
                            else
                            {
                                if (CATOBS == 6)
                                {
                                    Symbol = "DANGER01";
                                    Sounding = true;
                                }
                                else
                                {
                                    if ((WATLEV == 1) || (WATLEV == 2))
                                    {
                                        Symbol = "OBSTRN11";
                                    }
                                    else if ((WATLEV == 4) || (WATLEV == 5))
                                    {
                                        Symbol = "DANGER03";
                                        Sounding = true;
                                    }
                                    else
                                    {
                                        Symbol = "DANGER01";
                                        Sounding = true;
                                    }
                                }
                            }

                            if (Sounding)
                            {
                                linker.Sounding = true;
                                linker.Sounding_Symbol = Select_SNDFRM04(TECSOU, QUASOU, STATUS, linker.Danger_Accuracy);
                            }
                        }

                        draw_command.SY ??= new List<DCC.SY>();
                        draw_command.SY.Add(new DCC.SY() {
                            Index = SymbolTable.Table.TryGetValue(Symbol, out int Symbol_Index) ? Symbol_Index : -1,
                            Angle = 0.0f,
                        });
                    }
                    break;
                case 2:
                    {
                        if (VALSOU != float.MaxValue)
                        {
                            linker.Sounding = true;
                            linker.Sounding_Symbol = Select_SNDFRM04(TECSOU, QUASOU, STATUS, linker.Danger_Accuracy);
                        }
                    }
                    break;
                case 3:
                    {
                        if (VALSOU != float.MaxValue)
                        {
                            linker.Sounding = true;
                            linker.Sounding_Symbol = Select_SNDFRM04(TECSOU, QUASOU, STATUS, linker.Danger_Accuracy);
                        }
                        else
                        {
                            if (CATOBS == 6)
                            {
                                draw_command.AP ??= new List<DCC.AP>();
                                draw_command.AP.Add(new DCC.AP() {
                                    Index = PatternTable.Table.TryGetValue("FOULAR01", out int Pattern_Index) ? Pattern_Index : -1,
                                });

                                draw_command.LS ??= new List<DCC.LS>();
                                draw_command.LS.Add(new DCC.LS() {
                                    Pen_Type = 2,
                                    Pen_Width = 2,
                                    Pen_ColorIndex = ColorTable.Table.TryGetValue("CHBLK", out int Color_Index) ? Color_Index : -1,
                                });
                            }
                            else
                            {
                                if ((WATLEV == 1) || (WATLEV == 2))
                                {
                                    draw_command.AC ??= new List<DCC.AC>();
                                    draw_command.AC.Add(new DCC.AC() {
                                        Index = ColorTable.Table.TryGetValue("CHBRN", out int Color_Index) ? Color_Index : -1,
                                        Trans = 0,
                                    });

                                    draw_command.LS ??= new List<DCC.LS>();
                                    draw_command.LS.Add(new DCC.LS() {
                                        Pen_Type = 0,
                                        Pen_Width = 2,
                                        Pen_ColorIndex = ColorTable.Table.TryGetValue("CSTLN", out int PenColor_Index) ? PenColor_Index : -1,
                                    });
                                }
                                else if ((WATLEV == 4) || (WATLEV == 5))
                                {
                                    draw_command.AC ??= new List<DCC.AC>();
                                    draw_command.AC.Add(new DCC.AC() {
                                        Index = ColorTable.Table.TryGetValue("DEPIT", out int Color_Index) ? Color_Index : -1,
                                        Trans = 0,
                                    });

                                    draw_command.LS ??= new List<DCC.LS>();
                                    draw_command.LS.Add(new DCC.LS() {
                                        Pen_Type = 1,
                                        Pen_Width = 2,
                                        Pen_ColorIndex = ColorTable.Table.TryGetValue("CSTLN", out int PenColor_Index) ? PenColor_Index : -1,
                                    });
                                }
                                else
                                {
                                    draw_command.AC ??= new List<DCC.AC>();
                                    draw_command.AC.Add(new DCC.AC() {
                                        Index = ColorTable.Table.TryGetValue("DEPVS", out int Color_Index) ? Color_Index : -1,
                                        Trans = 0,
                                    });

                                    draw_command.LS ??= new List<DCC.LS>();
                                    draw_command.LS.Add(new DCC.LS() {
                                        Pen_Type = 2,
                                        Pen_Width = 2,
                                        Pen_ColorIndex = ColorTable.Table.TryGetValue("CHBLK", out int PenColor_Index) ? PenColor_Index : -1,
                                    });
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private void Select_WRECKS05(DCC.Feature feature, DCC.FeatureLinker linker, Dictionary<uint, DCC.UndGroup> und_group, DCC.DrawCommand draw_command)
        {
            bool QUASOU = false;
            bool STATUS = false;
            bool TECSOU = false;

            float VALSOU = float.MaxValue;
            byte WATLEV = 255;
            byte EXPSOU = 255;
            byte CATWRK = 255;

            if (feature.ATTF != null)
            {
                foreach (DCC.ATTF ATTF in feature.ATTF)
                {
                    if (ATTF.ATVL.Length > 0)
                    {
                        switch (ATTF.ATTL)
                        {
                            case 125:
                                {
                                    IEnumerable<string> ATVL_Enumeration = ATTF.ATVL.Where(ATVL => int.TryParse(ATVL, out int QUASOU) && ((QUASOU == 3) || (QUASOU == 4) || (QUASOU == 5) || (QUASOU == 8) || (QUASOU == 9)));

                                    if (ATVL_Enumeration.Count() > 0)
                                    {
                                        QUASOU = true;
                                    }
                                }
                                break;
                            case 149:
                                {
                                    IEnumerable<string> ATVL_Enumeration = ATTF.ATVL.Where(ATVL => int.TryParse(ATVL, out int STATUS) && (STATUS == 18));

                                    if (ATVL_Enumeration.Count() > 0)
                                    {
                                        STATUS = true;
                                    }
                                }
                                break;
                            case 156:
                                {
                                    IEnumerable<string> ATVL_Enumeration = ATTF.ATVL.Where(ATVL => int.TryParse(ATVL, out int TECSOU) && ((TECSOU == 4) || (TECSOU == 6)));

                                    if (ATVL_Enumeration.Count() > 0)
                                    {
                                        TECSOU = true;
                                    }
                                }
                                break;
                            case 179:
                                {
                                    if (ATTF.ATVL[0] != "9999")
                                    {
                                        float.TryParse(ATTF.ATVL[0], out VALSOU);
                                    }
                                }
                                break;
                            case 93:
                                {
                                    if (ATTF.ATVL[0] != "9999")
                                    {
                                        byte.TryParse(ATTF.ATVL[0], out EXPSOU);
                                    }
                                }
                                break;
                            case 187:
                                {
                                    if (ATTF.ATVL[0] != "9999")
                                    {
                                        byte.TryParse(ATTF.ATVL[0], out WATLEV);
                                    }
                                }
                                break;
                            case 71:
                                {
                                    if (ATTF.ATVL[0] != "9999")
                                    {
                                        byte.TryParse(ATTF.ATVL[0], out CATWRK);
                                    }
                                }
                                break;
                        }
                    }
                }
            }

            (float LEAST, float SEABED, float MAX) DEPTH = Select_DEPVAL02(linker, und_group, WATLEV, EXPSOU);
            linker.DRVAL1 = DEPTH.MAX;
            linker.Danger_Accuracy = !Select_QUAPNT02(linker);

            float Danger_DEPTH = float.MaxValue;

            if (VALSOU != float.MaxValue)
            {
                Danger_DEPTH = VALSOU;
            }
            else
            {
                if (DEPTH.LEAST == float.MaxValue)
                {
                    if (CATWRK != 255)
                    {
                        if (CATWRK == 1)
                        {
                            Danger_DEPTH = 20.1f;

                            if (DEPTH.SEABED != float.MaxValue)
                            {
                                DEPTH.LEAST = DEPTH.SEABED - 66.0f;

                                if (DEPTH.LEAST >= 20.1f)
                                {
                                    Danger_DEPTH = DEPTH.LEAST;
                                }
                            }
                        }
                        else
                        {
                            Danger_DEPTH = -15.0f;
                        }
                    }
                    else
                    {
                        if (WATLEV != 255)
                        {
                            if ((WATLEV == 3) || (WATLEV == 5))
                            {
                                Danger_DEPTH = 0.0f;
                            }
                            else
                            {
                                Danger_DEPTH = -15.0f;
                            }
                        }
                        else
                        {
                            Danger_DEPTH = -15.0f;
                        }
                    }
                }
                else
                {
                    Danger_DEPTH = DEPTH.LEAST;
                }
            }

            linker.Danger_DEPTH = Danger_DEPTH;
            linker.VALSOU = VALSOU;
            linker.Danger_WATLEV_1_2 = ((WATLEV == 1) || (WATLEV == 2));

            if (VALSOU != float.MaxValue)
            {
                linker.Sounding = true;
                linker.Sounding_Symbol = Select_SNDFRM04(TECSOU, QUASOU, STATUS, linker.Danger_Accuracy);
            }
            else
            {
                if (linker.FRID.PRIM == 1)
                {
                    string Symbol;

                    if ((CATWRK == 1) && (WATLEV == 3))
                    {
                        Symbol = "WRECKS04";
                    }
                    else if ((CATWRK == 2) && (WATLEV == 3))
                    {
                        Symbol = "WRECKS05";
                    }
                    else if ((CATWRK == 4) || (CATWRK == 5) || (WATLEV == 1) || (WATLEV == 2) || (WATLEV == 4) || (WATLEV == 5))
                    {
                        Symbol = "WRECKS01";
                    }
                    else
                    {
                        Symbol = "WRECKS05";
                    }

                    draw_command.SY ??= new List<DCC.SY>();
                    draw_command.SY.Add(new DCC.SY() {
                        Index = SymbolTable.Table.TryGetValue(Symbol, out int Symbol_Index) ? Symbol_Index : -1,
                        Angle = 0.0f,
                    });
                }
                else
                {
                    switch (WATLEV)
                    {
                        case 1:
                        case 2:
                            {
                                draw_command.AC ??= new List<DCC.AC>();
                                draw_command.AC.Add(new DCC.AC() {
                                    Index = ColorTable.Table.TryGetValue("CHBRN", out int Color_Index) ? Color_Index : -1,
                                    Trans = 0,
                                });

                                draw_command.LS ??= new List<DCC.LS>();
                                draw_command.LS.Add(new DCC.LS() {
                                    Pen_Type = 0,
                                    Pen_Width = 2,
                                    Pen_ColorIndex = ColorTable.Table.TryGetValue("CSTLN", out int PenColor_Index) ? PenColor_Index : -1,
                                });
                            }
                            break;
                        case 4:
                            {
                                draw_command.AC ??= new List<DCC.AC>();
                                draw_command.AC.Add(new DCC.AC() {
                                    Index = ColorTable.Table.TryGetValue("DEPIT", out int Color_Index) ? Color_Index : -1,
                                    Trans = 0,
                                });

                                draw_command.LS ??= new List<DCC.LS>();
                                draw_command.LS.Add(new DCC.LS() {
                                    Pen_Type = 1,
                                    Pen_Width = 2,
                                    Pen_ColorIndex = ColorTable.Table.TryGetValue("CSTLN", out int PenColor_Index) ? PenColor_Index : -1,
                                });
                            }
                            break;
                        case 3:
                        case 5:
                            {
                                draw_command.AC ??= new List<DCC.AC>();
                                draw_command.AC.Add(new DCC.AC() {
                                    Index = ColorTable.Table.TryGetValue("DEPVS", out int Color_Index) ? Color_Index : -1,
                                    Trans = 0,
                                });

                                draw_command.LS ??= new List<DCC.LS>();
                                draw_command.LS.Add(new DCC.LS() {
                                    Pen_Type = 2,
                                    Pen_Width = 2,
                                    Pen_ColorIndex = ColorTable.Table.TryGetValue("CSTLN", out int PenColor_Index) ? PenColor_Index : -1,
                                });
                            }
                            break;
                        default:
                            {
                                draw_command.AC ??= new List<DCC.AC>();
                                draw_command.AC.Add(new DCC.AC() {
                                    Index = ColorTable.Table.TryGetValue("DEPVS", out int Color_Index) ? Color_Index : -1,
                                    Trans = 0,
                                });

                                draw_command.LS ??= new List<DCC.LS>();
                                draw_command.LS.Add(new DCC.LS() {
                                    Pen_Type = 2,
                                    Pen_Width = 2,
                                    Pen_ColorIndex = ColorTable.Table.TryGetValue("CSTLN", out int PenColor_Index) ? PenColor_Index : -1,
                                });
                            }
                            break;
                    }
                }
            }
        }

        private void Select_SOUNDG03(DCC.Feature feature, DCC.FeatureLinker linker)
        {
            bool QUASOU = false;
            bool STATUS = false;
            bool TECSOU = false;
            bool QUAPOS = false;

            if (feature.ATTF != null)
            {
                foreach (DCC.ATTF ATTF in feature.ATTF)
                {
                    switch (ATTF.ATTL)
                    {
                        case 125:
                            {
                                IEnumerable<string> ATVL_Enumeration = ATTF.ATVL.Where(ATVL => int.TryParse(ATVL, out int QUASOU) && ((QUASOU == 3) || (QUASOU == 4) || (QUASOU == 5) || (QUASOU == 8) || (QUASOU == 9)));

                                if (ATVL_Enumeration.Count() > 0)
                                {
                                    QUASOU = true;
                                }
                            }
                            break;
                        case 149:
                            {
                                IEnumerable<string> ATVL_Enumeration = ATTF.ATVL.Where(ATVL => int.TryParse(ATVL, out int STATUS) && (STATUS == 18));

                                if (ATVL_Enumeration.Count() > 0)
                                {
                                    STATUS = true;
                                }
                            }
                            break;
                        case 156:
                            {
                                IEnumerable<string> ATVL_Enumeration = ATTF.ATVL.Where(ATVL => int.TryParse(ATVL, out int TECSOU) && ((TECSOU == 4) || (TECSOU == 6)));

                                if (ATVL_Enumeration.Count() > 0)
                                {
                                    TECSOU = true;
                                }
                            }
                            break;
                    }
                }
            }

            if (linker.Shape?.Count > 0)
            {
                DCC.ShapeLinker Shape_Linker = linker.Shape[0];

                if (linker.FRID.OBJL == 129)
                {
                    QUAPOS = (Shape_Linker.Vector_3D.ATVL == 0);
                }
                else
                {
                    QUAPOS = (Shape_Linker.Vector_2D.ATVL == 0);
                }

                if (Shape_Linker.Vector_3D.SG3D != null)
                {
                    foreach (DCC.SG3D SG3D in Shape_Linker.Vector_3D.SG3D)
                    {
                        linker.Sound ??= new List<DCC.Sound>();
                        linker.Sound.Add(new DCC.Sound() {
                            XCOO = SG3D.XCOO,
                            YCOO = SG3D.YCOO,
                            Sounding = SG3D.VE3D / 10.0f,
                            Sounding_Symbol = Select_SNDFRM04(TECSOU, QUASOU, STATUS, QUAPOS),
                            Manual = false,
                        });
                    }
                }
            }
        }

        private int Select_RESARE04(DCC.Feature feature, DCC.FeatureLinker linker, DCC.DrawCommand draw_command)
        {
            int Result = 0;

            bool[] RESTRN = new bool[28];
            bool[] CATREA = new bool[29];

            bool RESTRN_Exist = false;
            bool CATREA_Exist = false;

            if (feature.ATTF != null)
            {
                foreach (DCC.ATTF ATTF in feature.ATTF)
                {
                    switch (ATTF.ATTL)
                    {
                        case 56:
                            {
                                foreach (string ATVL in ATTF.ATVL)
                                {
                                    if (int.TryParse(ATVL, out int CATREA_Index) && (0 <= CATREA_Index) && (CATREA_Index < CATREA.Length))
                                    {
                                        CATREA[CATREA_Index] = true;
                                    }
                                }

                                CATREA_Exist = true;
                            }
                            break;
                        case 131:
                            {
                                foreach (string ATVL in ATTF.ATVL)
                                {
                                    if (int.TryParse(ATVL, out int RESTRN_Index) && (0 <= RESTRN_Index) && (RESTRN_Index < RESTRN.Length))
                                    {
                                        RESTRN[RESTRN_Index] = true;
                                    }
                                }

                                RESTRN_Exist = true;
                            }
                            break;
                    }
                }
            }

            DCC.SY SY = new DCC.SY()
            {
                Index = -1,
                Angle = 0.0f,
            };

            if (RESTRN_Exist)
            {
                if (RESTRN[7] || RESTRN[8] || RESTRN[14])
                {
                    string Symbol = RESTRN switch {
                        _ when (RESTRN[1] || RESTRN[2] || RESTRN[3] || RESTRN[4] || RESTRN[5] || RESTRN[6] || RESTRN[13] || RESTRN[16] || RESTRN[17] || RESTRN[23] || RESTRN[24] || RESTRN[25] || RESTRN[26] || RESTRN[27]) => "ENTRES61",
                        _ when (CATREA_Exist && (CATREA[1] || CATREA[8] || CATREA[9] || CATREA[12] || CATREA[14] || CATREA[18] || CATREA[19] || CATREA[21] || CATREA[24] || CATREA[25] || CATREA[26])) => "ENTRES61",
                        _ when (RESTRN[9] || RESTRN[10] || RESTRN[11] || RESTRN[12] || RESTRN[15] || RESTRN[18] || RESTRN[19] || RESTRN[20] || RESTRN[21] || RESTRN[22]) => "ENTRES71",
                        _ when (CATREA_Exist && (CATREA[4] || CATREA[5] || CATREA[6] || CATREA[7] || CATREA[10] || CATREA[20] || CATREA[22] || CATREA[23])) => "ENTRES71",
                        _ => "ENTRES51",
                    };

                    SY.Index = SymbolTable.Table.TryGetValue(Symbol, out int Symbol_Index) ? Symbol_Index : -1;

                    draw_command.SY ??= new List<DCC.SY>();
                    draw_command.SY.Add(SY);

                    linker.Display_Group = 6;

                    Result = 1;
                }
                else if (RESTRN[1] || RESTRN[2])
                {
                    string Symbol = RESTRN switch {
                        _ when (RESTRN[3] || RESTRN[4] || RESTRN[5] || RESTRN[6] || RESTRN[13] || RESTRN[16] || RESTRN[17] || RESTRN[23] || RESTRN[24] || RESTRN[25] || RESTRN[26] || RESTRN[27]) => "ACHRES61",
                        _ when (CATREA_Exist && (CATREA[1] || CATREA[8] || CATREA[9] || CATREA[12] || CATREA[14] || CATREA[18] || CATREA[19] || CATREA[21] || CATREA[24] || CATREA[25] || CATREA[26])) => "ACHRES61",
                        _ when (RESTRN[9] || RESTRN[10] || RESTRN[11] || RESTRN[12] || RESTRN[15] || RESTRN[18] || RESTRN[19] || RESTRN[20] || RESTRN[21] || RESTRN[22]) => "ACHRES71",
                        _ when (CATREA_Exist && (CATREA[4] || CATREA[5] || CATREA[6] || CATREA[7] || CATREA[10] || CATREA[20] || CATREA[22] || CATREA[23])) => "ACHRES71",
                        _ => "ACHRES51",
                    };

                    SY.Index = SymbolTable.Table.TryGetValue(Symbol, out int Symbol_Index) ? Symbol_Index : -1;

                    draw_command.SY ??= new List<DCC.SY>();
                    draw_command.SY.Add(SY);

                    linker.Display_Group = 6;

                    Result = 2;
                }
                else if (RESTRN[3] || RESTRN[4] || RESTRN[5] || RESTRN[6] || RESTRN[24])
                {
                    string Symbol = RESTRN switch {
                        _ when (RESTRN[13] || RESTRN[16] || RESTRN[17] || RESTRN[23] || RESTRN[25] || RESTRN[26] || RESTRN[27]) => "FSHRES61",
                        _ when (CATREA_Exist && (CATREA[1] || CATREA[8] || CATREA[9] || CATREA[12] || CATREA[14] || CATREA[18] || CATREA[19] || CATREA[21] || CATREA[24] || CATREA[25] || CATREA[26])) => "FSHRES61",
                        _ when (RESTRN[9] || RESTRN[10] || RESTRN[11] || RESTRN[12] || RESTRN[15] || RESTRN[18] || RESTRN[19] || RESTRN[20] || RESTRN[21] || RESTRN[22]) => "FSHRES71",
                        _ when (CATREA_Exist && (CATREA[4] || CATREA[5] || CATREA[6] || CATREA[7] || CATREA[10] || CATREA[20] || CATREA[22] || CATREA[23])) => "FSHRES71",
                        _ => "FSHRES51",
                    };

                    SY.Index = SymbolTable.Table.TryGetValue(Symbol, out int Symbol_Index) ? Symbol_Index : -1;

                    draw_command.SY ??= new List<DCC.SY>();
                    draw_command.SY.Add(SY);

                    linker.Display_Group = 6;

                    Result = 3;
                }
                else if (RESTRN[13] || RESTRN[16] || RESTRN[17] || RESTRN[23] || RESTRN[25] || RESTRN[26] || RESTRN[27])
                {
                    string Symbol = RESTRN switch {
                        _ when (RESTRN[9] || RESTRN[10] || RESTRN[11] || RESTRN[12] || RESTRN[15] || RESTRN[18] || RESTRN[19] || RESTRN[20] || RESTRN[21] || RESTRN[22]) => "CTYARE71",
                        _ when (CATREA_Exist && (CATREA[4] || CATREA[5] || CATREA[6] || CATREA[7] || CATREA[10] || CATREA[20] || CATREA[22] || CATREA[23])) => "CTYARE71",
                        _ => "CTYARE51",
                    };

                    SY.Index = SymbolTable.Table.TryGetValue(Symbol, out int Symbol_Index) ? Symbol_Index : -1;

                    draw_command.SY ??= new List<DCC.SY>();
                    draw_command.SY.Add(SY);

                    linker.Display_Group = 6;

                    Result = 4;
                }
                else
                {
                    string Symbol = (RESTRN[9] || RESTRN[10] || RESTRN[11] || RESTRN[12] || RESTRN[15] || RESTRN[18] || RESTRN[19] || RESTRN[20] || RESTRN[21] || RESTRN[22]) ? "INFARE51" : "RSRDEF51";

                    SY.Index = SymbolTable.Table.TryGetValue(Symbol, out int Symbol_Index) ? Symbol_Index : -1;

                    draw_command.SY ??= new List<DCC.SY>();
                    draw_command.SY.Add(SY);

                    Result = 5;
                }
            }
            else
            {
                string Symbol;

                if (CATREA_Exist)
                {
                    if (CATREA[1] || CATREA[8] || CATREA[9] || CATREA[12] || CATREA[14] || CATREA[18] || CATREA[19] || CATREA[21] || CATREA[24] || CATREA[25] || CATREA[26])
                    {
                        if (CATREA[4] || CATREA[5] || CATREA[6] || CATREA[7] || CATREA[10] || CATREA[20] || CATREA[22] || CATREA[23])
                        {
                            Symbol = "CTYARE71";
                        }
                        else
                        {
                            Symbol = "CTYARE51";
                        }
                    }
                    else
                    {
                        if (CATREA[4] || CATREA[5] || CATREA[6] || CATREA[7] || CATREA[10] || CATREA[20] || CATREA[22] || CATREA[23])
                        {
                            Symbol = "INFARE51";
                        }
                        else
                        {
                            Symbol = "RSRDEF51";
                        }
                    }
                }
                else
                {
                    Symbol = "RSRDEF51";
                }

                SY.Index = SymbolTable.Table.TryGetValue(Symbol, out int Symbol_Index) ? Symbol_Index : -1;

                draw_command.SY ??= new List<DCC.SY>();
                draw_command.SY.Add(SY);

                Result = 6;
            }

            return Result;
        }

        private void Select_QUAPOS01(DCC.Feature feature, DCC.FeatureLinker linker, DCC.DrawCommand draw_command)
        {
            if (linker.FRID.PRIM == 2)
            {
                Select_QUALIN01(feature, linker, draw_command);
            }
            else
            {
                bool Low_Accuracy = Select_QUAPNT02(linker);

                if (!Low_Accuracy)
                {
                    draw_command.SY ??= new List<DCC.SY>();
                    draw_command.SY.Add(new DCC.SY() {
                        Index = SymbolTable.Table.TryGetValue("LOWACC01", out int Symbol_Index) ? Symbol_Index : -1,
                        Angle = 0.0f,
                    });
                }
            }
        }

        private void Select_SYMINS02(DCC.Feature feature, DCC.FeatureLinker linker, DCC.DrawCommand draw_command)
        {
            string ATVL = "";

            if (feature.ATTF != null)
            {
                IEnumerable<DCC.ATTF> ATTF_Enumeration = feature.ATTF.Where(ATTF => (ATTF.ATTL == 192) && (ATTF.ATVL.Length > 0));

                if (ATTF_Enumeration.Count() > 0)
                {
                    ATVL = ATTF_Enumeration.First().ATVL[0];
                }
            }

            var temp = ATVL.Split(";");
            foreach(var subATVL in temp)
            {
                Regex Command_Regex = new Regex(@"(\b[A-Z]{2}\b)\((.+)\)");

                if (!Command_Regex.IsMatch(subATVL))
                {
                    switch (linker.FRID.PRIM)
                    {
                        case 1:
                            {
                                draw_command.SY ??= new List<DCC.SY>();
                                draw_command.SY.Add(new DCC.SY()
                                {
                                    Index = SymbolTable.Table.TryGetValue("QUESMRK1", out int Symbol_Index) ? Symbol_Index : -1,
                                    Angle = 0.0f,
                                });
                            }
                            break;
                        case 2:
                            {
                                draw_command.LC ??= new List<DCC.LC>();
                                draw_command.LC.Add(new DCC.LC()
                                {
                                    Index = LineTable.Table.TryGetValue("QUESMRK1", out int Line_Index) ? Line_Index : -1,
                                });
                            }
                            break;
                        case 3:
                            {
                                draw_command.AP ??= new List<DCC.AP>();
                                draw_command.AP.Add(new DCC.AP()
                                {
                                    Index = PatternTable.Table.TryGetValue("QUESMRK1", out int Pattern_Index) ? Pattern_Index : -1,
                                });
                            }
                            break;
                    }
                }
                else
                {
                    ENC.Lookup Lookup = new ENC.Lookup();
                    LookupTable.Extract_LookupCommand(subATVL, Command_Regex, ref Lookup);

                    if (Lookup.AC != null)
                    {
                        foreach (ENC.AC AC in Lookup.AC)
                        {
                            draw_command.AC ??= new List<DCC.AC>();
                            draw_command.AC.Add(new DCC.AC()
                            {
                                Index = ColorTable.Table.TryGetValue(AC.Acronym, out int Color_Index) ? Color_Index : -1,
                                Trans = AC.Trans,
                            });
                        }
                    }

                    if (Lookup.AP != null)
                    {
                        foreach (ENC.AP AP in Lookup.AP)
                        {
                            draw_command.AP ??= new List<DCC.AP>();

                            if (PatternTable.Table.TryGetValue(AP.Acronym, out int AP_Index))
                            {
                                draw_command.AP.Add(new DCC.AP()
                                {
                                    Index = AP_Index,
                                });
                            }
                            else
                            {
                                draw_command.AP.Add(new DCC.AP()
                                {
                                    Index = PatternTable.Table.TryGetValue("QUESMRK1", out int QUESMRK_Index) ? QUESMRK_Index : -1,
                                });
                            }
                        }
                    }

                    if (Lookup.LS != null)
                    {
                        foreach (ENC.LS LS in Lookup.LS)
                        {
                            draw_command.LS ??= new List<DCC.LS>();
                            draw_command.LS.Add(new DCC.LS()
                            {
                                Pen_Type = LS.Pen_Type,
                                Pen_Width = LS.Pen_Width,
                                Pen_ColorIndex = ColorTable.Table.TryGetValue(LS.Pen_ColorAcronym, out int Color_Index) ? Color_Index : -1,
                            });
                        }
                    }

                    if (Lookup.LC != null)
                    {
                        foreach (ENC.LC LC in Lookup.LC)
                        {
                            draw_command.LC ??= new List<DCC.LC>();

                            if (LineTable.Table.TryGetValue(LC.Acronym, out int LC_Index))
                            {
                                draw_command.LC.Add(new DCC.LC()
                                {
                                    Index = LC_Index,
                                });
                            }
                            else
                            {
                                draw_command.LC.Add(new DCC.LC()
                                {
                                    Index = LineTable.Table.TryGetValue("QUESMRK1", out int QUESMRK_Index) ? QUESMRK_Index : -1,
                                });
                            }
                        }
                    }

                    if (Lookup.SY != null)
                    {
                        foreach (ENC.SY SY in Lookup.SY)
                        {
                            string Acronym = SY.Acronym switch
                            {
                                "OSPONE02" => "OSPONE03",
                                "VECWTR01" => "VECWTR02",
                                "VECWTR21" => "VECWTR22",
                                "OSPSIX02" => "OSPSIX03",
                                "AISATN01" => "AISATN02",
                                "AISTRN01" => "AISTRN03",
                                "AISTRN02" => "AISTRN04",
                                "ARPONE01" => "ARPONE02",
                                "ARPSIX01" => "ARPSIX02",
                                "ARPATG01" => "ARPATG02",
                                "OWNSHP01" => "OWNSHP02",
                                "VECGND01" => "VECGND02",
                                "AISLST01" => "AISLST02",
                                "AISSLP01" => "AISSLP02",
                                "AISVES01" => "AISVES02",
                                "AISDGR01" => "AISDGR02",
                                _ => SY.Acronym,
                            };

                            draw_command.SY ??= new List<DCC.SY>();

                            if (SymbolTable.Table.TryGetValue(Acronym, out int Symbol_Index))
                            {
                                draw_command.SY.Add(new DCC.SY()
                                {
                                    Index = Symbol_Index,
                                    Angle = float.TryParse(SY.Degree, out float Angle) ? Angle : 0.0f,
                                });
                            }
                            else
                            {
                                draw_command.SY.Add(new DCC.SY()
                                {
                                    Index = SymbolTable.Table.TryGetValue("QUESMRK1", out int QUESMRK_Index) ? QUESMRK_Index : -1,
                                    Angle = 0.0f,
                                });
                            }
                        }
                    }

                    if (Lookup.TE != null)
                    {
                        foreach (ENC.TE TE in Lookup.TE)
                        {
                            string Text;
                            string NationalText;

                            if (TE.Element == "OBJNAM")
                            {
                                Text = !string.IsNullOrEmpty(feature.OBJNAM) ? Interpret_TE(TE.Format, feature.OBJNAM) : "";
                                NationalText = !string.IsNullOrEmpty(feature.NOBJNM) ? Interpret_TE(TE.Format, feature.NOBJNM) : Text;
                            }
                            else
                            {
                                Text = !string.IsNullOrEmpty(TE.Element) ? Interpret_TE(TE.Format, TE.Element) : "";
                                NationalText = Text;
                            }

                            if (!string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(NationalText))
                            {
                                draw_command.TX ??= new List<DCC.TX>();
                                draw_command.TX.Add(new DCC.TX()
                                {
                                    Text = Text.Replace("\r", null).Replace("\n", null),
                                    NationalText = NationalText.Replace("\r", null).Replace("\n", null),
                                    Align = (byte)((TE.Font_HJUST * 10) + TE.Font_VJUST),
                                    Offset = TE.Font_Offset,
                                    Text_Group = (byte)TE.Font_Group,
                                    Text_ColorIndex = (byte)(ColorTable.Table.TryGetValue(TE.Font_ColorAcronym, out int Color_Index) ? Color_Index : 255),
                                });
                            }
                        }
                    }

                    if (Lookup.TX != null)
                    {
                        foreach (ENC.TX TX in Lookup.TX)
                        {
                            string Text;
                            string NationalText;

                            if (TX.Element == "OBJNAM")
                            {
                                Text = feature.OBJNAM;
                                NationalText = !string.IsNullOrEmpty(feature.NOBJNM) ? feature.NOBJNM : feature.OBJNAM;
                            }
                            else
                            {
                                Text = TX.Text;
                                NationalText = TX.Text;
                            }

                            if (!string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(NationalText))
                            {
                                draw_command.TX ??= new List<DCC.TX>();
                                draw_command.TX.Add(new DCC.TX()
                                {
                                    Text = Text.Replace("\r", null).Replace("\n", null),
                                    NationalText = NationalText.Replace("\r", null).Replace("\n", null),
                                    Align = (byte)((TX.Font_HJUST * 10) + TX.Font_VJUST),
                                    Offset = TX.Font_Offset,
                                    Text_Group = (byte)TX.Font_Group,
                                    Text_ColorIndex = (byte)(ColorTable.Table.TryGetValue(TX.Font_ColorAcronym, out int Color_Index) ? Color_Index : 255),
                                });
                            }
                        }
                    }
                }
            }
        }

        private void Select_RESTRN01(DCC.Feature feature, DCC.FeatureLinker linker, DCC.DrawCommand draw_command)
        {
            bool[] RESTRN = new bool[28];
            bool RESTRN_Exist = false;

            if (feature.ATTF != null)
            {
                foreach (DCC.ATTF ATTF in feature.ATTF)
                {
                    if (ATTF.ATTL == 131)
                    {
                        RESTRN_Exist = true;

                        foreach (string ATVL in ATTF.ATVL)
                        {
                            if (int.TryParse(ATVL, out int RESTRN_Index) && (0 <= RESTRN_Index) && (RESTRN_Index < RESTRN.Length))
                            {
                                RESTRN[RESTRN_Index] = true;
                            }
                        }
                    }
                }
            }

            if (RESTRN_Exist)
            {
                draw_command.SY ??= new List<DCC.SY>();
                draw_command.SY.Add(new DCC.SY() {
                    Index = Select_RESCSP02(RESTRN),
                    Angle = 0.0f,
                });
            }
        }

        private void Select_CLRLIN01(DCC.Feature feature, DCC.FeatureLinker linker, DCC.DrawCommand draw_command)
        {
            if (linker.FRID.PRIM == 2)
            {
                float ORIENT = 90.0f;

                if (feature.ATTF != null)
                {
                    foreach (DCC.ATTF ATTF in feature.ATTF)
                    {
                        if ((ATTF.ATTL == 117) && (ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] != "9999"))
                        {
                            float.TryParse(ATTF.ATVL[0], out ORIENT);
                            break;
                        }
                    }
                }

                draw_command.LS ??= new List<DCC.LS>();
                draw_command.LS.Add(new DCC.LS() {
                    Pen_Type = 0,
                    Pen_Width = 1,
                    Pen_ColorIndex = ColorTable.Table.TryGetValue("NINFO", out int Color_Index) ? Color_Index : -1,
                });

                draw_command.SY ??= new List<DCC.SY>();
                draw_command.SY.Add(new DCC.SY() {
                    Index = SymbolTable.Table.TryGetValue("CLRLIN01", out int Symbol_Index) ? Symbol_Index : -1,
                    Angle = ORIENT,
                });
            }
        }

        private void Select_DATCVR02(DCC.Feature feature, DCC.FeatureLinker linker, DCC.DrawCommand draw_command)
        {
            int CATCOV = -1;

            if (feature.ATTF != null)
            {
                foreach (DCC.ATTF ATTF in feature.ATTF)
                {
                    if ((ATTF.ATTL == 18) && (ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] != "9999"))
                    {
                        int.TryParse(ATTF.ATVL[0], out CATCOV);
                        break;
                    }
                }
            }

            if (CATCOV != 1)
            {
                draw_command.LC ??= new List<DCC.LC>();
                draw_command.LC.Add(new DCC.LC() {
                    Index = 32,
                });
            }
        }


        private string Select_LITDSN02(bool[] catlit, bool[] colour, bool[] status, bool catlit_exist, bool colour_exist, bool status_exist, byte litchr, string siggrp, float sigper, float height, float valnmr)
        {
            StringBuilder LITDSN = new StringBuilder();

            if (catlit_exist)
            {
                if (catlit[1]) { LITDSN.Append("Dir "); }
                if (catlit[5]) { LITDSN.Append("Aero "); }
                if (catlit[7]) { LITDSN.Append("Fog Det Lt "); }
            }

            string LITCHR = litchr switch {
                1 => "F",
                2 => "FI",
                3 => "LFI",
                4 => "Q",
                5 => "VQ",
                6 => "UQ",
                7 => "Iso",
                8 => "Oc",
                9 => "IQ",
                10 => "IVQ",
                11 => "IUQ",
                12 => "Mo",
                13 => "FFI",
                14 => "FI+LFI",
                15 => "OcFI",
                16 => "FLFI",
                17 => "AIOc",
                18 => "AILFI",
                19 => "AIFI",
                20 => "AI",
                25 => "Q+LFI",
                26 => "VQ+LFI",
                27 => "UQ+LFI",
                28 => "AI",
                29 => "AIF FI",
                _ => string.Empty,
            };

            if (!string.IsNullOrEmpty(siggrp))
            {
                string[] SIGGRP_Segment = siggrp.Split(')');
                int Multiple = LITCHR.IndexOf('+');

                if (Multiple > -1)
                {
                    LITDSN.Append(LITCHR.Substring(0, Multiple));

                    if (SIGGRP_Segment.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(SIGGRP_Segment[0]) && (SIGGRP_Segment[0] != "(") && (SIGGRP_Segment[0] != "(1"))
                        {
                            LITDSN.Append(SIGGRP_Segment[0]);
                            LITDSN.Append(')');
                        }
                    }

                    LITDSN.Append('+');
                    LITDSN.Append(LITCHR.Substring(Multiple + 1));

                    if (SIGGRP_Segment.Length > 1)
                    {
                        if (!string.IsNullOrEmpty(SIGGRP_Segment[1]) && (SIGGRP_Segment[1] != "(") && (SIGGRP_Segment[1] != "(1"))
                        {
                            LITDSN.Append(SIGGRP_Segment[1]);
                            LITDSN.Append(')');
                        }
                    }
                }
                else
                {
                    LITDSN.Append(LITCHR);

                    for (int i = 0; i < SIGGRP_Segment.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(SIGGRP_Segment[i]) && (SIGGRP_Segment[i] != "(") && (SIGGRP_Segment[i] != "(1"))
                        {
                            LITDSN.Append(SIGGRP_Segment[i]);
                            LITDSN.Append(')');
                        }
                    }
                }
            }
            else
            {
                LITDSN.Append(LITCHR);
            }

            if (colour_exist)
            {
                if (colour[1]) { LITDSN.Append('W'); }
                if (colour[3]) { LITDSN.Append('R'); }
                if (colour[4]) { LITDSN.Append('G'); }
                if (colour[6]) { LITDSN.Append('Y'); }
            }

            LITDSN.Append(' ');

            if (sigper != float.MaxValue)
            {
                LITDSN.Append($"{sigper:0.#}s");
            }

            if (height != float.MaxValue)
            {
                LITDSN.Append($"{height:0.#}m");
            }

            if (valnmr != float.MaxValue)
            {
                LITDSN.Append($"{valnmr:0.#}M");
            }

            if (status_exist)
            {
                if (status[2]) { LITDSN.Append("(occas)"); }
                if (status[7]) { LITDSN.Append("(temp)"); }
                if (status[8]) { LITDSN.Append("(priv)"); }
                if (status[11]) { LITDSN.Append("(exting)"); }
                if (status[17]) { LITDSN.Append("(U)"); }
            }

            return LITDSN.ToString();
        }

        private (float LEAST, float SEABED, float MAX) Select_DEPVAL02(DCC.FeatureLinker linker, Dictionary<uint, DCC.UndGroup> und_group, byte watlev, byte expsou)
        {
            (float LEAST, float SEABED, float MAX) DEPTH = (float.MaxValue, float.MaxValue, float.MaxValue);

            bool Cover = false;

            if (und_group.TryGetValue(linker.FRID.RCID, out DCC.UndGroup Und_Group))
            {
                DEPTH.LEAST = Und_Group.Depth.Least;
                DEPTH.MAX = Und_Group.Depth.Maximum;
                Cover = Und_Group.Cover;
            }

            if (DEPTH.LEAST != float.MaxValue)
            {
                if ((watlev == 3) && ((expsou == 1) || (expsou == 3)))
                {
                    DEPTH.SEABED = DEPTH.LEAST;
                }
                else
                {
                    DEPTH.SEABED = DEPTH.LEAST;
                    DEPTH.LEAST = float.MaxValue;
                }
            }
            else
            {
                if (Cover)
                {
                    DEPTH.SEABED = -2.0f;
                }
            }

            return DEPTH;
        }

        private bool Select_QUAPNT02(DCC.FeatureLinker linker)
        {
            bool Accuracy = true;

            if (linker.Shape?.Count > 0)
            {
                if (linker.FRID.PRIM == 1)
                {
                    uint QUAPOS = (linker.FRID.OBJL == 129) ? linker.Shape[0].Vector_3D.ATVL : linker.Shape[0].Vector_2D.ATVL;

                    if (QUAPOS != 1)
                    {
                        Accuracy = false;
                    }
                }
                else
                {
                    foreach (DCC.ShapeLinker Shape_Linker in linker.Shape)
                    {
                        if (Shape_Linker.Edge != null)
                        {
                            IEnumerable<DCC.EdgeLinker> EdgeLinker_Enumeration = Shape_Linker.Edge.Where(Edge_Linker => Edge_Linker.ATVL != 1);

                            if (EdgeLinker_Enumeration.Count() > 0)
                            {
                                Accuracy = false;
                                break;
                            }
                        }
                    }
                }
            }

            return Accuracy;
        }

        private string Select_SNDFRM04(bool tecsou, bool quasou, bool status, bool danger_accuracy)
        {
            StringBuilder Sounding = new StringBuilder();

            if (tecsou)
            {
                Sounding.Append("B1");
            }

            if (quasou || status)
            {
                Sounding.Append("C2");
            }
            else
            {
                if (danger_accuracy)
                {
                    Sounding.Append("C2");
                }
            }

            return Sounding.ToString();
        }

        private void Select_QUALIN01(DCC.Feature feature, DCC.FeatureLinker linker, DCC.DrawCommand draw_command)
        {
            if (linker.FRID.OBJL == 30)
            {
                byte CONRAD = 255;

                if (feature.ATTF != null)
                {
                    foreach (DCC.ATTF ATTF in feature.ATTF)
                    {
                        if (ATTF.ATTL == 82)
                        {
                            if ((ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] != "9999"))
                            {
                                byte.TryParse(ATTF.ATVL[0], out CONRAD);
                            }

                            break;
                        }
                    }
                }

                draw_command.LS ??= new List<DCC.LS>();

                if ((CONRAD != 255) && (CONRAD == 1))
                {
                    draw_command.LS.Add(new DCC.LS() {
                        Pen_Type = 0,
                        Pen_Width = 3,
                        Pen_ColorIndex = ColorTable.Table.TryGetValue("CHMGF", out int CHMGF_Index) ? CHMGF_Index : -1,
                    });
                }

                draw_command.LS.Add(new DCC.LS() {
                    Pen_Type = 0,
                    Pen_Width = 1,
                    Pen_ColorIndex = ColorTable.Table.TryGetValue("CSTLN", out int CSTLN_Index) ? CSTLN_Index : -1,
                });
            }
            else
            {
                draw_command.LS ??= new List<DCC.LS>();
                draw_command.LS.Add(new DCC.LS() {
                    Pen_Type = 0,
                    Pen_Width = 1,
                    Pen_ColorIndex = ColorTable.Table.TryGetValue("CSTLN", out int Color_Index) ? Color_Index : -1,
                });
            }
        }

        private int Select_RESCSP02(bool[] restrn)
        {
            int Result = -1;

            if (restrn[7] || restrn[8] || restrn[14])
            {
                string Symbol = restrn switch {
                    _ when (restrn[1] || restrn[2] || restrn[3] || restrn[4] || restrn[5] || restrn[6] || restrn[13] || restrn[16] || restrn[17] || restrn[23] || restrn[24] || restrn[25] || restrn[26] || restrn[27]) => "ENTRES61",
                    _ when (restrn[9] || restrn[10] || restrn[11] || restrn[12] || restrn[15] || restrn[18] || restrn[19] || restrn[20] || restrn[21] || restrn[22]) => "ENTRES71",
                    _ => "ENTRES51",
                };

                Result = SymbolTable.Table.TryGetValue(Symbol, out int Symbol_Index) ? Symbol_Index : -1;
            }
            else
            {
                if (restrn[1] || restrn[2])
                {
                    string Symbol = restrn switch {
                        _ when (restrn[3] || restrn[4] || restrn[5] || restrn[6] || restrn[13] || restrn[16] || restrn[17] || restrn[23] || restrn[24] || restrn[25] || restrn[26] || restrn[27]) => "ACHRES61",
                        _ when (restrn[9] || restrn[10] || restrn[11] || restrn[12] || restrn[15] || restrn[18] || restrn[19] || restrn[20] || restrn[21] || restrn[22]) => "ACHRES71",
                        _ => "ACHRES51",
                    };

                    Result = SymbolTable.Table.TryGetValue(Symbol, out int Symbol_Index) ? Symbol_Index : -1;
                }
                else
                {
                    if (restrn[3] || restrn[4] || restrn[5] || restrn[6] || restrn[24])
                    {
                        string Symbol = restrn switch {
                            _ when (restrn[13] || restrn[16] || restrn[17] || restrn[23] || restrn[25] || restrn[26] || restrn[27]) => "FSHRES61",
                            _ when (restrn[9] || restrn[10] || restrn[11] || restrn[12] || restrn[15] || restrn[18] || restrn[19] || restrn[20] || restrn[21] || restrn[22]) => "FSHRES71",
                            _ => "FSHRES51",
                        };

                        Result = SymbolTable.Table.TryGetValue(Symbol, out int Symbol_Index) ? Symbol_Index : -1;
                    }
                    else
                    {
                        if (restrn[13] || restrn[16] || restrn[17] || restrn[23] || restrn[25] || restrn[26] || restrn[27])
                        {
                            if (restrn[9] || restrn[10] || restrn[11] || restrn[12] || restrn[15] || restrn[18] || restrn[19] || restrn[20] || restrn[21] || restrn[22])
                            {
                                Result = SymbolTable.Table.TryGetValue("CTYARE71", out int Symbol_Index) ? Symbol_Index : -1;
                            }
                            else
                            {
                                Result = SymbolTable.Table.TryGetValue("CTYARE51", out int Symbol_Index) ? Symbol_Index : -1;
                            }
                        }
                        else
                        {
                            if (restrn[9] || restrn[10] || restrn[11] || restrn[12] || restrn[15] || restrn[18] || restrn[19] || restrn[20] || restrn[21] || restrn[22])
                            {
                                Result = SymbolTable.Table.TryGetValue("INFARE51", out int Symbol_Index) ? Symbol_Index : -1;
                            }
                            else
                            {
                                Result = SymbolTable.Table.TryGetValue("RSRDEF51", out int Symbol_Index) ? Symbol_Index : -1;
                            }
                        }
                    }
                }
            }

            return Result;
        }


        private string Interpret_TE(string format, string data, int type = 0)
        {
            StringBuilder TE_Builder = new StringBuilder();
            string[] Format_Segment = format.Split(' ');

            for (int i = 0; i < Format_Segment.Length; i++)
            {
                int N = Format_Segment[i].IndexOf('%');

                if (N != -1)
                {
                    if (type == 0)
                    {
                        TE_Builder.Append((i < (Format_Segment.Length - 1)) ? $"{data} " : data);
                    }
                    else
                    {
                        float.TryParse(data, out float Data);

                        TE_Builder.Append(ApplyFormat(Data, Format_Segment[i]));

                        if (i < (Format_Segment.Length - 1)) { TE_Builder.Append(' '); }
                    }
                }
                else
                {
                    TE_Builder.Append((i < (Format_Segment.Length - 1)) ? $"{Format_Segment[i]} " : Format_Segment[i]);
                }
            }

            return TE_Builder.ToString();
        }

        private string ApplyFormat(object value, string cppFormat)
        {
            if (value == null) return string.Empty;

            // 정규표현식 업데이트: (lf|f|d|s) -> s 추가
            var match = Regex.Match(cppFormat, @"%?(-)?(0)?(\d+)?(\.\d+)?(lf|f|d|s)");

            if (!match.Success) return value.ToString();

            bool leftAlign = match.Groups[1].Success; // '-' 기호 여부
            bool zeroPad = match.Groups[2].Success;   // '0' 기호 여부
            string widthStr = match.Groups[3].Value;
            string precisionStr = match.Groups[4].Value.Replace(".", "");
            string type = match.Groups[5].Value;      // lf, f, d, s 중 하나

            int width = string.IsNullOrEmpty(widthStr) ? 0 : int.Parse(widthStr);
            // 좌측 정렬인 경우 C#에서는 width를 음수로 표현합니다.
            int finalWidth = leftAlign ? -width : width;

            // --- 문자열(%s) 처리 로직 ---
            if (type == "s")
            {
                // C#의 복합 포맷팅 {0,10} 기능을 이용해 폭과 정렬을 해결합니다.
                return string.Format($"{{0,{finalWidth}}}", value.ToString());
            }

            // --- 숫자(lf, f, d) 처리 로직 ---
            double numValue = Convert.ToDouble(value);
            int precision = string.IsNullOrEmpty(precisionStr) ? 0 : int.Parse(precisionStr);
            string formatSpecifier = "F" + precision;

            if (zeroPad && width > 0 && !leftAlign)
            {
                string pattern = new string('0', width);
                if (precision > 0) pattern += "." + new string('0', precision);
                return numValue.ToString(pattern);
            }

            return string.Format($"{{0,{finalWidth}:{formatSpecifier}}}", numValue);
        }
    }
}