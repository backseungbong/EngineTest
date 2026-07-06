using Legacy.ECM_Core.Catalogue;
using Legacy.ECM_Core.Chart;
using Legacy.ECM_Core.Definition;
using Legacy.ECM_Core.Table;
using System.IO;
using System.Text;

namespace Legacy.ECM_Core.Component
{
    public partial class ChartComposer
    {
        public bool Convert_Chart(DetectionChart chart)
        {
            if (chart.Linked)
            {
                try
                {
                    if (!Using_Chart1)
                    {
                        Convert_EncChart(chart, chart.Name.Contains("KRINDEX"));
                    }
                    else
                    {
                        Convert_EncChart1(chart);
                    }

                    return true;
                }
                catch (Exception e)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }


        private void Convert_EncChart(DetectionChart chart, bool index_chart)
        {
            SencChart Senc_Chart = new SencChart()
            {
                Name = chart.Name,
            };

            (double North, double South, double East, double West) Coverage = (
                North: -90.0 * 10000000.0,
                South: 90.0 * 10000000.0,
                East: -180.0 * 10000000.0,
                West: 180.0 * 10000000.0
            );

            if ((chart.Feature != null) && (chart.FeatureLinker_Collection != null))
            {
                for (int i = 0; i < chart.Feature.Count; i++) // 여기서도 입증이 되는 게, 결국 feature와 linker list가 똑같은 순서로 복사가 되어있는 것처럼 다루고 있음 (만들어낼 때부터 갯수가 같을 수밖에 없으므로 여기서 일단 linker 대신 feature의 index로)
                {
                    DCC.Feature Feature = chart.Feature[i];
                    DCC.FeatureLinker Linker = chart.FeatureLinker_Collection[Feature.FRID.RCID];

                    if (Linker.FRID.OBJL == 302)
                    {
                        Coverage = Compose_Coverage(Senc_Chart, Feature, Linker, Coverage);
                    }
                    else
                    {
                        if (!index_chart)
                        {
                            Compose_Layer(Senc_Chart, Feature, Linker);
                        }
                        else
                        {
                            Compose_IndexLayer(Senc_Chart, Feature, Linker);
                        }
                    }
                }
            }

            double Width = Math.Abs(Coverage.East - Coverage.West) / 2.0;
            double Height = Math.Abs(Coverage.North - Coverage.South) / 2.0;

            (int X, int Y) Pivot = (
                X: (int)(Coverage.West + Width),
                Y: (int)(Coverage.South + Height)
            );

            chart.Boundary = Coverage;

            ChartCatalogue.Set_Catalogue(chart, Pivot);


            Serialize_SENC(Senc_Chart);
            Serialize_Search(Senc_Chart);
            Serialize_Update(chart, index_chart);
        }

        private void Convert_EncChart1(DetectionChart chart)
        {
            SencChart Senc_Chart = new SencChart()
            {
                Name = chart.Name,
            };

            (double North, double South, double East, double West) Coverage = (
                North: -90.0 * 10000000.0,
                South: 90.0 * 10000000.0,
                East: -180.0 * 10000000.0,
                West: 180.0 * 10000000.0
            );

            if ((chart.Feature != null) && (chart.FeatureLinker_Collection != null))
            {
                for (int i = 0; i < chart.Feature.Count; i++)
                {
                    DCC.Feature Feature = chart.Feature[i];
                    DCC.FeatureLinker Linker = chart.FeatureLinker_Collection[Feature.FRID.RCID];

                    if (Linker.FRID.OBJL == 302)
                    {
                        Coverage = Compose_Coverage(Senc_Chart, Feature, Linker, Coverage);
                    }
                    else
                    {
                        Compose_Layer(Senc_Chart, Feature, Linker);
                    }
                }
            }

            double Width = Math.Abs(Coverage.East - Coverage.West) / 2.0;
            double Height = Math.Abs(Coverage.North - Coverage.South) / 2.0;

            (int X, int Y) Pivot = (
                X: (int)(Coverage.West + Width),
                Y: (int)(Coverage.South + Height)
            );

            chart.Boundary = Coverage;

            Chart1Catalogue.Set_Catalogue(chart, Pivot);


            Serialize_SENC(Senc_Chart);
        }


        public (double North, double South, double East, double West) Compose_Coverage(SencChart chart, DCC.Feature feature, DCC.FeatureLinker linker, (double North, double South, double East, double West) coverage)
        {
            (double North, double South, double East, double West) Result = coverage;


            int CATCOV = -1;

            if (feature.ATTF != null)
            {
                foreach (DCC.ATTF ATTF in feature.ATTF)
                {
                    if (ATTF.ATTL == 18)
                    {
                        if (ATTF.ATVL.Length > 0)
                        {
                            int.TryParse(ATTF.ATVL[0], out CATCOV);
                        }

                        break;
                    }
                }
            }

            SencCover Cover = new SencCover()
            {
                Cover2 = (CATCOV != 1),
            };

            if (linker.Shape?.Count > 0)
            {
                DCC.ShapeLinker Shape_Linker = linker.Shape[0];

                if (Shape_Linker.Point != null)
                {
                    Cover.Point = new List<SCE.SencPoint>();

                    foreach (DCC.SG2D SG2D in Shape_Linker.Point)
                    {
                        Cover.Point.Add(new SCE.SencPoint() {
                            X = SG2D.XCOO,
                            Y = SG2D.YCOO,
                        });

                        if (Result.West > SG2D.XCOO) { Result.West = SG2D.XCOO; }
                        if (Result.East < SG2D.XCOO) { Result.East = SG2D.XCOO; }
                        if (Result.South > SG2D.YCOO) { Result.South = SG2D.YCOO; }
                        if (Result.North < SG2D.YCOO) { Result.North = SG2D.YCOO; }
                    }
                }
            }

            chart.Cover ??= new List<SencCover>();
            chart.Cover.Add(Cover);


            return Result;
        }

        public void Compose_Layer(SencChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            if ((300 <= linker.FRID.OBJL) && (linker.FRID.OBJL <= 312))
            {
                Compose_Meta(chart, feature, linker);
            }
            else
            {
                switch (linker.FRID.OBJL)
                {
                    case OBJL.DEPARE: { Compose_DEPARE(chart, feature, linker); } break;
                    case OBJL.LNDARE: { Compose_LNDARE(chart, feature, linker); } break;
                    case OBJL.DRGARE: { Compose_DRGARE(chart, feature, linker); } break;
                    case OBJL.UNSARE: { Compose_UNSARE(chart, feature, linker); } break;
                    case OBJL.DEPCNT: { Compose_DEPCNT(chart, feature, linker); } break;
                    case OBJL.OBSTRN:
                    case OBJL.UWTROC: { Compose_OBSTRN(chart, feature, linker); } break;
                    case OBJL.WRECKS: { Compose_WRECKS(chart, feature, linker); } break;
                    case OBJL.LIGHTS: { Compose_LIGHTS(chart, feature, linker); } break;
                    case OBJL.SOUNDG: { Compose_SOUNDG(chart, feature, linker); } break;
                    case OBJL.SLCONS: { Compose_SLCONS(chart, feature, linker); } break;
                    default: { Compose_OBJECT(chart, feature, linker); } break;
                }
            }
        }

        public void Compose_IndexLayer(SencChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            if (linker.FRID.OBJL == OBJL.LNDARE)
            {
                Compose_LNDARE(chart, feature, linker);
            }
        }


        private void Compose_Meta(SencChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            SencMeta Meta = new SencMeta()
            {
                FRID = linker.FRID,
                Display_Group = linker.Display_Group,
                Radar_Overlay = linker.Radar_Overlay,
                Update_Type = feature.Update_Type,
                Viewing_Group = linker.Group_Layer,
                Low_Accuracy = false,
                CSCALE = -1,
            };

            if (linker.Edge_Masked) { Meta.Viewing_Group += 100; }

            if (feature.FRID.OBJL == 308)
            {
                if (feature.ATTF != null)
                {
                    foreach (DCC.ATTF ATTF in feature.ATTF)
                    {
                        if ((ATTF.ATTL == 72) && (ATTF.ATVL.Length > 0) && int.TryParse(ATTF.ATVL[0], out int CATZOC))
                        {
                            if ((3 < CATZOC) && (CATZOC < 7))
                            {
                                Meta.Low_Accuracy = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (feature.Is_HighlightDocument()) { Meta.Highlight = 10; }

            if (linker.FRID.OBJL == 301)
            {
                if (feature.ATTF != null)
                {
                    foreach (DCC.ATTF ATTF in feature.ATTF)
                    {
                        if ((ATTF.ATTL == 80) && (ATTF.ATVL.Length > 0) && int.TryParse(ATTF.ATVL[0], out int CSCALE))
                        {
                            Meta.CSCALE = CSCALE;
                            break;
                        }
                    }
                }

                if (Meta.CSCALE != -1)
                {
                    if (ChartCatalogue.Catalogue.TryGetValue(chart.Name, out ENC.Chart Catalogue) && (Catalogue.Scale == Meta.CSCALE))
                    {
                        Meta.CSCALE = -1;
                    }
                }
            }

            (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Vertex = Import_SencVertex(linker);

            if (Vertex.Point != null) { Meta.Point = Vertex.Point; };
            if (Vertex.Edge != null) { Meta.Edge = Vertex.Edge; };
            if (Vertex.Shape != null) { Meta.Shape = Vertex.Shape; };

            if (linker.Draw_Command != null) { Meta.Command = linker.Draw_Command; }

            Compose_Text(chart, linker);

            chart.Meta ??= new List<SencMeta>();
            chart.Meta.Add(Meta);
        }

        private void Compose_DEPARE(SencChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            if (linker.FRID.PRIM != 3) { return; }


            SencDepare DEPARE = new SencDepare()
            {
                RCID = linker.FRID.RCID,
                Display_Group = linker.Display_Group,
                Radar_Overlay = linker.Radar_Overlay,
                Minimum_Scale = linker.Minimum_Scale,
                Update_Type = feature.Update_Type,
            };

            (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Vertex = Import_SencVertex(linker);

            if (Vertex.Point != null) { DEPARE.Point = Vertex.Point; };
            if (Vertex.Edge != null) { DEPARE.Edge = Vertex.Edge; };
            if (Vertex.Shape != null) { DEPARE.Shape = Vertex.Shape; };

            if (linker.Draw_Command != null) { DEPARE.Command = linker.Draw_Command; }

            Compose_Text(chart, linker);

            if (linker.Shape != null)
            {
                foreach (DCC.ShapeLinker Shape_Linker in linker.Shape)
                {
                    if (Shape_Linker.Edge != null)
                    {
                        foreach (DCC.EdgeLinker Edge_Linker in Shape_Linker.Edge)
                        {
                            DEPARE.Edge_Attribute ??= new List<DCC.EdgeAttribute>();
                            DEPARE.Edge_Attribute.Add(Edge_Linker.Edge_Attribute);
                        }
                    }
                }
            }

            if (feature.ATTF != null)
            {
                float DRVAL1 = float.MaxValue;
                float DRVAL2 = float.MaxValue;

                foreach (DCC.ATTF ATTF in feature.ATTF)
                {
                    if (ATTF.ATVL.Length > 0)
                    {
                        switch (ATTF.ATTL)
                        {
                            case 87: { float.TryParse(ATTF.ATVL[0], out DRVAL1); } break;
                            case 88: { float.TryParse(ATTF.ATVL[0], out DRVAL2); } break;
                        }
                    }
                }

                if (DRVAL1 == float.MaxValue) { DRVAL1 = -1.0f; }
                if (DRVAL2 == float.MaxValue) { DRVAL2 = DRVAL1 + 0.01f; }

                DEPARE.DRVAL1 = DRVAL1;
                DEPARE.DRVAL2 = DRVAL2;
            }
            else
            {
                DEPARE.DRVAL1 = float.MaxValue;
                DEPARE.DRVAL2 = float.MaxValue;
            }

            chart.DEPARE ??= new List<SencDepare>();
            chart.DEPARE.Add(DEPARE);

            Compose_Detection(chart, feature, linker, 1, DEPARE.DRVAL1);
        }

        private void Compose_LNDARE(SencChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            SencLndare LNDARE = new SencLndare()
            {
                RCID = linker.FRID.RCID,
                PRIM = linker.FRID.PRIM,
                Display_Group = linker.Display_Group,
                Radar_Overlay = linker.Radar_Overlay,
                Minimum_Scale = linker.Minimum_Scale,
                Pivot = linker.Pivot,
                Information = feature.Has_INFORM(),
                Update_Type = feature.Update_Type,
            };

            if (linker.FRID.PRIM != 1)
            {
                (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Vertex = Import_SencVertex(linker);

                if (Vertex.Point != null) { LNDARE.Point = Vertex.Point; };
                if (Vertex.Edge != null) { LNDARE.Edge = Vertex.Edge; };
                if (Vertex.Shape != null) { LNDARE.Shape = Vertex.Shape; };
            }

            if (linker.Draw_Command != null) { LNDARE.Command = linker.Draw_Command; }

            Compose_Text(chart, linker);

            chart.LNDARE ??= new List<SencLndare>();
            chart.LNDARE.Add(LNDARE);

            Compose_Detection(chart, feature, linker, 0);
        }

        private void Compose_DRGARE(SencChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            SencDrgare DRGARE = new SencDrgare()
            {
                RCID = linker.FRID.RCID,
                Display_Group = linker.Display_Group,
                Radar_Overlay = linker.Radar_Overlay,
                Minimum_Scale = linker.Minimum_Scale,
                Information = feature.Has_INFORM(),
                Update_Type = feature.Update_Type,
            };

            (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Vertex = Import_SencVertex(linker);

            if (Vertex.Point != null) { DRGARE.Point = Vertex.Point; };
            if (Vertex.Edge != null) { DRGARE.Edge = Vertex.Edge; };
            if (Vertex.Shape != null) { DRGARE.Shape = Vertex.Shape; };

            if (linker.Draw_Command != null) { DRGARE.Command = linker.Draw_Command; }

            Compose_Text(chart, linker);

            if (linker.Shape != null)
            {
                foreach (DCC.ShapeLinker Shape_Linker in linker.Shape)
                {
                    if (Shape_Linker.Edge != null)
                    {
                        foreach (DCC.EdgeLinker Edge_Linker in Shape_Linker.Edge)
                        {
                            DRGARE.Edge_Attribute ??= new List<DCC.EdgeAttribute>();
                            DRGARE.Edge_Attribute.Add(Edge_Linker.Edge_Attribute);
                        }
                    }
                }
            }


            float DRVAL1 = float.MaxValue;

            bool[] RESTRN = new bool[28];
            bool RESTRN_Exist = false;

            if (feature.ATTF != null)
            {
                foreach (DCC.ATTF ATTF in feature.ATTF)
                {
                    switch (ATTF.ATTL)
                    {
                        case 87:
                            if (ATTF.ATVL.Length > 0)
                            {
                                float.TryParse(ATTF.ATVL[0], out DRVAL1);
                            }
                            break;
                        case 131:
                            {
                                foreach (string ATVL in ATTF.ATVL)
                                {
                                    if (byte.TryParse(ATVL, out byte RESTRN_Index) && (0 <= RESTRN_Index) && (RESTRN_Index < RESTRN.Length))
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

            if (DRVAL1 == float.MaxValue) { DRVAL1 = -1.0f; }

            DRGARE.DRVAL1 = DRVAL1;


            DCC.AP AP = new DCC.AP()
            {
                Index = PatternTable.Table.TryGetValue("DRGARE01", out int Pattern_Index) ? Pattern_Index : -1,
            };

            DCC.LS LS = new DCC.LS()
            {
                Pen_Type = 1,
                Pen_Width = 1,
                Pen_ColorIndex = ColorTable.Table.TryGetValue("CHGRF", out int Color_Index) ? Color_Index : -1,
            };

            if ((DRGARE.Command == null) || (DRGARE.Command?.Count == 0))
            {
                DCC.DrawCommand Draw_Command = new DCC.DrawCommand()
                {
                    AP = new List<DCC.AP>() { AP },
                    LS = new List<DCC.LS>() { LS },
                };

                if (RESTRN_Exist)
                {
                    Draw_Command.SY = new List<DCC.SY>() {
                        new DCC.SY() {
                            Index = Select_RESCSP02(RESTRN),
                            Angle = 0.0f,
                        },
                    };
                }

                DRGARE.Command ??= new List<DCC.DrawCommand>();
                DRGARE.Command.Add(Draw_Command);
            }
            else if (DRGARE.Command?.Count == 1)
            {
                DCC.DrawCommand Draw_Command = DRGARE.Command[0];

                Draw_Command.AP ??= new List<DCC.AP>();
                Draw_Command.AP.Add(AP);

                Draw_Command.LS ??= new List<DCC.LS>();
                Draw_Command.LS.Add(LS);

                if (RESTRN_Exist)
                {
                    Draw_Command.SY ??= new List<DCC.SY>();
                    Draw_Command.SY.Add(new DCC.SY() {
                        Index = Select_RESCSP02(RESTRN),
                        Angle = 0.0f,
                    });
                }
            }
            else if (DRGARE.Command?.Count == 2)
            {
                DCC.DrawCommand Draw_Command_0 = DRGARE.Command[0];
                DCC.DrawCommand Draw_Command_1 = DRGARE.Command[1];

                Draw_Command_0.AP ??= new List<DCC.AP>();
                Draw_Command_0.AP.Add(AP);

                Draw_Command_1.AP ??= new List<DCC.AP>();
                Draw_Command_1.AP.Add(AP);

                Draw_Command_0.LS ??= new List<DCC.LS>();
                Draw_Command_0.LS.Add(LS);

                Draw_Command_1.LS ??= new List<DCC.LS>();
                Draw_Command_1.LS.Add(LS);

                if (RESTRN_Exist)
                {
                    DCC.SY SY = new DCC.SY()
                    {
                        Index = Select_RESCSP02(RESTRN),
                        Angle = 0.0f,
                    };

                    Draw_Command_0.SY ??= new List<DCC.SY>();
                    Draw_Command_0.SY.Add(SY);

                    Draw_Command_1.SY ??= new List<DCC.SY>();
                    Draw_Command_1.SY.Add(SY);
                }
            }


            chart.DRGARE ??= new List<SencDrgare>();
            chart.DRGARE.Add(DRGARE);

            Compose_Detection(chart, feature, linker, 1, DRGARE.DRVAL1);
        }

        private void Compose_UNSARE(SencChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            SencUnsare UNSARE = new SencUnsare()
            {
                RCID = linker.FRID.RCID,
                Display_Group = linker.Display_Group,
                Radar_Overlay = linker.Radar_Overlay,
                Minimum_Scale = linker.Minimum_Scale,
                Update_Type = feature.Update_Type,
            };

            (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Vertex = Import_SencVertex(linker);

            if (Vertex.Point != null) { UNSARE.Point = Vertex.Point; };
            if (Vertex.Edge != null) { UNSARE.Edge = Vertex.Edge; };
            if (Vertex.Shape != null) { UNSARE.Shape = Vertex.Shape; };

            if (linker.Draw_Command != null) { UNSARE.Command = linker.Draw_Command; }

            Compose_Text(chart, linker);

            chart.UNSARE ??= new List<SencUnsare>();
            chart.UNSARE.Add(UNSARE);

            Compose_Detection(chart, feature, linker, 0);
        }

        private void Compose_DEPCNT(SencChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            SencDepcnt DEPCNT = new SencDepcnt()
            {
                RCID = linker.FRID.RCID,
                Display_Group = linker.Display_Group,
                Radar_Overlay = linker.Radar_Overlay,
                Minimum_Scale = linker.Minimum_Scale,
                Update_Type = feature.Update_Type,
            };

            if (feature.Get_ATVL(174, out string ATVL))
            {
                if (ATVL != "9999")
                {
                    float.TryParse(ATVL, out DEPCNT.VALDCO);
                }
                else
                {
                    DEPCNT.VALDCO = float.MaxValue;
                }
            }

            (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Vertex = Import_SencVertex(linker);

            if (Vertex.Point != null) { DEPCNT.Point = Vertex.Point; };
            if (Vertex.Edge != null) { DEPCNT.Edge = Vertex.Edge; };
            if (Vertex.Shape != null) { DEPCNT.Shape = Vertex.Shape; };

            chart.DEPCNT ??= new List<SencDepcnt>();
            chart.DEPCNT.Add(DEPCNT);
        }

        private void Compose_OBSTRN(SencChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            SencObstrn OBSTRN = new SencObstrn()
            {
                RCID = linker.FRID.RCID,
                PRIM = linker.FRID.PRIM,
                OBJL = linker.FRID.OBJL,
                Pivot = linker.Pivot,
                Display_Group = linker.Display_Group,
                Radar_Overlay = linker.Radar_Overlay,
                Minimum_Scale = linker.Minimum_Scale,
                Update_Type = feature.Update_Type,
                Viewing_Group = linker.Group_Layer,
                Information = feature.Has_INFORM(),
                Danger_DEPTH = linker.Danger_DEPTH,
                DRVAL1 = linker.DRVAL1,
                VALSOU = linker.VALSOU,
                Danger_Accuracy = linker.Danger_Accuracy,
                Danger_WATLEV_1_2 = linker.Danger_WATLEV_1_2,
                Sounding = linker.Sounding,
                Sounding_Symbol = linker.Sounding_Symbol,
            };

            if (linker.Edge_Masked) { OBSTRN.Viewing_Group += 100; }

            (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Vertex = Import_SencVertex(linker);

            if (Vertex.Point != null) { OBSTRN.Point = Vertex.Point; };
            if (Vertex.Edge != null) { OBSTRN.Edge = Vertex.Edge; };
            if (Vertex.Shape != null) { OBSTRN.Shape = Vertex.Shape; };

            if (linker.Draw_Command != null) { OBSTRN.Command = linker.Draw_Command; }

            Compose_Text(chart, linker);

            switch (OBSTRN.PRIM)
            {
                case 1:
                    {
                        chart.OBSTRN_P ??= new List<SencObstrn>();
                        chart.OBSTRN_P.Add(OBSTRN);
                    }
                    break;
                case 2:
                    {
                        chart.OBSTRN_L ??= new List<SencObstrn>();
                        chart.OBSTRN_L.Add(OBSTRN);
                    }
                    break;
                case 3:
                    {
                        chart.OBSTRN_A ??= new List<SencObstrn>();
                        chart.OBSTRN_A.Add(OBSTRN);
                    }
                    break;
            }

            Compose_Detection(chart, feature, linker, 4);
        }

        private void Compose_WRECKS(SencChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            SencObstrn WRECKS = new SencObstrn()
            {
                RCID = linker.FRID.RCID,
                PRIM = linker.FRID.PRIM,
                OBJL = linker.FRID.OBJL,
                Pivot = linker.Pivot,
                Display_Group = linker.Display_Group,
                Radar_Overlay = linker.Radar_Overlay,
                Minimum_Scale = linker.Minimum_Scale,
                Update_Type = feature.Update_Type,
                Viewing_Group = linker.Group_Layer,
                Information = feature.Has_INFORM(),
                Danger_DEPTH = linker.Danger_DEPTH,
                DRVAL1 = linker.DRVAL1,
                VALSOU = linker.VALSOU,
                Danger_Accuracy = linker.Danger_Accuracy,
                Danger_WATLEV_1_2 = linker.Danger_WATLEV_1_2,
                Sounding = linker.Sounding,
                Sounding_Symbol = linker.Sounding_Symbol,
            };

            if (linker.Edge_Masked) { WRECKS.Viewing_Group += 100; }

            (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Vertex = Import_SencVertex(linker);

            if (Vertex.Point != null) { WRECKS.Point = Vertex.Point; };
            if (Vertex.Edge != null) { WRECKS.Edge = Vertex.Edge; };
            if (Vertex.Shape != null) { WRECKS.Shape = Vertex.Shape; };

            if (linker.Draw_Command != null) { WRECKS.Command = linker.Draw_Command; }

            Compose_Text(chart, linker);

            switch (WRECKS.PRIM)
            {
                case 1:
                    {
                        chart.WRECKS_P ??= new List<SencObstrn>();
                        chart.WRECKS_P.Add(WRECKS);
                    }
                    break;
                case 3:
                    {
                        chart.WRECKS_A ??= new List<SencObstrn>();
                        chart.WRECKS_A.Add(WRECKS);
                    }
                    break;
            }

            Compose_Detection(chart, feature, linker, 4);
        }

        private void Compose_LIGHTS(SencChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            SencLights LIGHTS = new SencLights()
            {
                RCID = linker.FRID.RCID,
                Pivot = linker.Pivot,
                Display_Group = linker.Display_Group,
                Radar_Overlay = linker.Radar_Overlay,
                Minimum_Scale = linker.Minimum_Scale,
                Update_Type = feature.Update_Type,
                Information = feature.Has_INFORM(),
                ORIENT = linker.ORIENT,
                VALNMR = linker.VALNMR,
                SECTR1 = linker.SECTR1,
                SECTR2 = linker.SECTR2,
                CATLIT_8_11 = linker.CATLIT_8_11,
                CATLIT_9 = linker.CATLIT_9,
                CATLIT_1_16 = linker.CATLIT_1_16,
                LITVIS_3_7_8 = linker.LITVIS_3_7_8,
                COLOUR = linker.COLOUR,
                Flare_At_45_Degrees = linker.Flare_At_45_Degrees,
                All_Round_Light = linker.All_Round_Light,
                LITDSN = linker.LITDSN,
                Extended_Arc_Radius = linker.Extended_Arc_Radius,
                Radius_26mm = linker.Radius_26mm,
            };

            chart.LIGHTS ??= new List<SencLights>();
            chart.LIGHTS.Add(LIGHTS);
        }

        private void Compose_SOUNDG(SencChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            SencSoundg SOUNDG = new SencSoundg()
            {
                RCID = linker.FRID.RCID,
                Display_Group = linker.Display_Group,
                Radar_Overlay = linker.Radar_Overlay,
                Minimum_Scale = linker.Minimum_Scale,
                Update_Type = feature.Update_Type,
            };

            if (linker.Sound != null) { SOUNDG.Sound = linker.Sound; }

            Compose_Text(chart, linker);

            chart.SOUNDG ??= new List<SencSoundg>();
            chart.SOUNDG.Add(SOUNDG);

            Compose_Detection(chart, feature, linker, 5);
        }

        private void Compose_SLCONS(SencChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            SencSlcons SLCONS = new SencSlcons()
            {
                RCID = linker.FRID.RCID,
                PRIM = linker.FRID.PRIM,
                Pivot = linker.Pivot,
                Display_Group = linker.Display_Group,
                Radar_Overlay = linker.Radar_Overlay,
                Minimum_Scale = linker.Minimum_Scale,
                Update_Type = feature.Update_Type,
                Information = feature.Has_INFORM(),
            };

            (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Vertex = Import_SencVertex(linker);

            if (Vertex.Point != null) { SLCONS.Point = Vertex.Point; };
            if (Vertex.Edge != null) { SLCONS.Edge = Vertex.Edge; };
            if (Vertex.Shape != null) { SLCONS.Shape = Vertex.Shape; };

            if (linker.Draw_Command != null) { SLCONS.Command = linker.Draw_Command; }

            Compose_Text(chart, linker);

            if (linker.Shape != null)
            {
                foreach (DCC.ShapeLinker Shape_Linker in linker.Shape)
                {
                    if (Shape_Linker.Edge != null)
                    {
                        foreach (DCC.EdgeLinker Edge_Linker in Shape_Linker.Edge)
                        {
                            SLCONS.Edge_Command ??= new List<DCC.EdgeCommand>();
                            SLCONS.Edge_Command.Add(Edge_Linker.Edge_Command);
                        }
                    }
                }
            }

            switch (SLCONS.PRIM)
            {
                case 1:
                    {
                        chart.SLCONS_P ??= new List<SencSlcons>();
                        chart.SLCONS_P.Add(SLCONS);
                    }
                    break;
                case 2:
                    {
                        chart.SLCONS_L ??= new List<SencSlcons>();
                        chart.SLCONS_L.Add(SLCONS);
                    }
                    break;
                case 3:
                    {
                        chart.SLCONS_A ??= new List<SencSlcons>();
                        chart.SLCONS_A.Add(SLCONS);
                    }
                    break;
            }

            Compose_Detection(chart, feature, linker, 0);
        }

        private void Compose_OBJECT(SencChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            SencObject OBJECT = new SencObject()
            {
                RCID = linker.FRID.RCID,
                PRIM = linker.FRID.PRIM,
                OBJL = linker.FRID.OBJL,
                Pivot = linker.Pivot,
                Display_Group = linker.Display_Group,
                Radar_Overlay = linker.Radar_Overlay,
                Minimum_Scale = linker.Minimum_Scale,
                Information = (byte)(feature.Has_INFORM() ? 1 : 0),
                Group_Layer = linker.Group_Layer,
                Reverse = feature.Is_Reverse(),
                Update_Type = feature.Update_Type,
            };

            OBJECT.Valid_Date.Start = ComposeObjectDate(feature.Valid_Date.Start, false);
            OBJECT.Valid_Date.End = ComposeObjectDate(feature.Valid_Date.End, true);

            if (linker.Edge_Masked) { OBJECT.Group_Layer += 100; }
            if (feature.Is_HighlightDocument()) { OBJECT.Information += 10; }


            (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Vertex = Import_SencVertex(linker);

            if (Vertex.Point != null) { OBJECT.Point = Vertex.Point; };
            if (Vertex.Edge != null) {
                OBJECT.Edge = Vertex.Edge;

                for (int i = 0; i < OBJECT.Edge.Count; i++)
                {
                    SCE.SencEdge edge = OBJECT.Edge[i];
                    edge.Reverse = feature.Is_Reverse(i);
                    OBJECT.Edge[i] = edge;
                }
            };
            if (Vertex.Shape != null) { OBJECT.Shape = Vertex.Shape; };

            if (linker.Edge_Mask != null) { OBJECT.Edge_Mask = linker.Edge_Mask; }

            if (linker.Draw_Command != null) { OBJECT.Command = linker.Draw_Command; }

            Compose_Text(chart, linker);

            if (Using_Chart1)
            {
                if (linker.FRID.OBJL == 72) // LNDEL(72)
                {
                    int ELEVAT = 0;

                    if (feature.ATTF != null)
                    {
                        IEnumerable<DCC.ATTF> ATTF_Enumeration = feature.ATTF.Where(ATTF => ATTF.ATTL == 90);

                        if (ATTF_Enumeration.Count() > 0)
                        {
                            DCC.ATTF ATTF = ATTF_Enumeration.First();

                            if (ATTF.ATVL.Length > 0)
                            {
                                ELEVAT = int.TryParse(ATTF.ATVL[0], out int ATVL) ? ATVL : 0;
                            }
                        }
                    }

                    OBJECT.Valid_Date.Start = ELEVAT;
                }

                if (OBJECT.Reverse)
                {
                    OBJECT.Update_Type = 4;
                }
            }


            switch (OBJECT.PRIM)
            {
                case 1:
                    {
                        chart.OBJECT_P ??= new List<SencObject>();
                        chart.OBJECT_P.Add(OBJECT);
                    }
                    break;
                case 2:
                    {
                        chart.OBJECT_L ??= new List<SencObject>();
                        chart.OBJECT_L.Add(OBJECT);
                    }
                    break;
                case 3:
                    {
                        chart.OBJECT_A ??= new List<SencObject>();
                        chart.OBJECT_A.Add(OBJECT);
                    }
                    break;
            }

            switch (linker.FRID.OBJL)
            {
                case 4:
                case 27:
                case 68:
                case 82:
                case 83:
                case 88:
                case 112:
                case 120:
                case 133:
                case 150: { Compose_Detection(chart, feature, linker, 2); } break;
                case 57:
                case 65:
                case 95: { Compose_Detection(chart, feature, linker, 0); } break;
                default: { Compose_Detection(chart, feature, linker, 3); } break;
            }
        }

        private int ComposeObjectDate(string featureDate, bool end)
        {
            switch (featureDate)
            {
                case string _ when featureDate.StartsWith("---"):
                    {
                        string Date = featureDate[3..];

                        if (Date.Length == 2)
                        {
                            if (int.TryParse(Date, out int DD))
                            {
                                return DD + 99999900;
                            }
                            else
                            {
                                return 0;
                            }
                        }
                        else
                        {
                            return 0;
                        }
                    }
                case string _ when featureDate.StartsWith("--"):
                    {
                        string Date = featureDate[2..];

                        if (Date.Length == 2)
                        {
                            if (int.TryParse(Date, out int MM))
                            {
                                return end ? ((MM * 100) + 99990031) : ((MM * 100) + 99990001);
                            }
                            else
                            {
                                return 0;
                            }
                        }
                        else if (Date.Length == 4)
                        {
                            if (int.TryParse(Date, out int MMDD))
                            {
                                return MMDD + 99990000;
                            }
                            else
                            {
                                return 0;
                            }
                        }
                        else
                        {
                            return 0;
                        }
                    }
                case string _ when featureDate.EndsWith("--"):
                    {
                        string Date = featureDate[..^2];

                        if (Date.Length == 6)
                        {
                            if (int.TryParse(Date, out int CCYYMM))
                            {
                                return end ? ((CCYYMM * 100) + 00000031) : ((CCYYMM * 100) + 00000001);
                            }
                            else
                            {
                                return 0;
                            }
                        }
                        else
                        {
                            return 0;
                        }
                    }
                default:
                    {
                        if (int.TryParse(featureDate, out int CCYYMMDD))
                        {
                            return CCYYMMDD;
                        }
                        else
                        {
                            return 0;
                        }
                    }
            }
        }


        private void Compose_Text(SencChart chart, DCC.FeatureLinker linker)
        {
            if (linker.Draw_Command != null)
            {
                IEnumerable<List<DCC.TX>> TX_Enumeration = linker.Draw_Command.Where(Command => Command.TX != null).Select<DCC.DrawCommand, List<DCC.TX>>(Command => Command.TX);

                if (TX_Enumeration.Count() > 0)
                {
                    SencText Text = new SencText()
                    {
                        RCID = linker.FRID.RCID,
                        Radar_Overlay = linker.Radar_Overlay,
                        TX = new List<DCC.TX>(),
                    };

                    foreach (List<DCC.TX> TX in TX_Enumeration)
                    {
                        Text.TX.AddRange(TX);
                    }

                    chart.Text ??= new List<SencText>();
                    chart.Text.Add(Text);
                }
            }
        }

        private void Compose_Detection(SencChart chart, DCC.Feature feature, DCC.FeatureLinker linker, byte type, float drval1 = float.MaxValue)
        {
            if (Using_Chart1) { return; }

            switch (type)
            {
                case 0: // Safety
                    {
                        SencSafety Safety = new SencSafety()
                        {
                            FRID = linker.FRID,
                            DRVAL1 = float.MaxValue,
                        };

                        (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Vertex = Import_SencVertex(linker);

                        if (Vertex.Point != null) { Safety.Point = Vertex.Point; };
                        if (Vertex.Edge != null) { Safety.Edge = Vertex.Edge; };
                        if (Vertex.Shape != null) { Safety.Shape = Vertex.Shape; };

                        chart.Safety ??= new List<SencSafety>();
                        chart.Safety.Add(Safety);
                    }
                    break;
                case 1: // Safety Depth
                    {
                        SencSafety Safety = new SencSafety()
                        {
                            FRID = linker.FRID,
                            DRVAL1 = drval1,
                        };

                        (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Vertex = Import_SencVertex(linker);

                        if (Vertex.Point != null) { Safety.Point = Vertex.Point; };
                        if (Vertex.Edge != null) { Safety.Edge = Vertex.Edge; };
                        if (Vertex.Shape != null) { Safety.Shape = Vertex.Shape; };

                        chart.Safety_Depth ??= new List<SencSafety>();
                        chart.Safety_Depth.Add(Safety);
                    }
                    break;
                case 2: // Special
                    {
                        SencSpecial Special = new SencSpecial()
                        {
                            FRID = linker.FRID,
                        };

                        if (linker.FRID.OBJL == 112)
                        {
                            bool[] RESTRN = new bool[28];
                            bool[] CATREA = new bool[29];

                            if (feature.ATTF != null)
                            {
                                foreach (DCC.ATTF ATTF in feature.ATTF)
                                {
                                    switch (ATTF.ATTL)
                                    {
                                        case 56:
                                            foreach (string ATVL in ATTF.ATVL)
                                            {
                                                if (int.TryParse(ATVL, out int CATREA_Index) && (0 <= CATREA_Index) && (CATREA_Index < 29))
                                                {
                                                    CATREA[CATREA_Index] = true;
                                                }
                                            }
                                            break;
                                        case 131:
                                            foreach (string ATVL in ATTF.ATVL)
                                            {
                                                if (int.TryParse(ATVL, out int RESTRN_Index) && (0 <= RESTRN_Index) && (RESTRN_Index < 28))
                                                {
                                                    RESTRN[RESTRN_Index] = true;
                                                }
                                            }
                                            break;
                                    }
                                }
                            }

                            Special.RESARE = RESTRN switch {
                                _ when (!RESTRN[14] && !CATREA[28]) => 1,
                                _ when RESTRN[14] => 2,
                                _ when CATREA[28] => 3,
                                _ => 0,
                            };
                        }
                        else
                        {
                            Special.RESARE = 0;
                        }

                        (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Vertex = Import_SencVertex(linker);

                        if (Vertex.Point != null) { Special.Point = Vertex.Point; };
                        if (Vertex.Edge != null) { Special.Edge = Vertex.Edge; };
                        if (Vertex.Shape != null) { Special.Shape = Vertex.Shape; };

                        chart.Special ??= new List<SencSpecial>();
                        chart.Special.Add(Special);
                    }
                    break;
                case 3: // Hazard
                    {
                        switch (linker.FRID.OBJL)
                        {
                            case ushort _ when ((4 < linker.FRID.OBJL) && (linker.FRID.OBJL < 10)):
                            case 11:
                            case ushort _ when ((13 < linker.FRID.OBJL) && (linker.FRID.OBJL < 20)):
                            case 21:
                            case 34:
                            case 39:
                            case 55:
                            case 66:
                            case 76:
                            case 77:
                            case 80:
                            case 84:
                            case 87:
                            case 89:
                            case 90:
                            case 93:
                            case 98:
                                {
                                    SencHazard Hazard = new SencHazard()
                                    {
                                        FRID = linker.FRID,
                                        DEPTH = float.MaxValue,
                                    };

                                    (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Vertex = Import_SencVertex(linker);

                                    if (Vertex.Point != null) { Hazard.Point = Vertex.Point; };
                                    if (Vertex.Edge != null) { Hazard.Edge = Vertex.Edge; };
                                    if (Vertex.Shape != null) { Hazard.Shape = Vertex.Shape; };

                                    chart.Hazard ??= new List<SencHazard>();
                                    chart.Hazard.Add(Hazard);
                                }
                                break;
                            case 163:
                                if (feature.ATTF != null)
                                {
                                    foreach (DCC.ATTF ATTF in feature.ATTF)
                                    {
                                        if (ATTF.ATTL == 191)
                                        {
                                            if ((ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] == "Virtual AtoN"))
                                            {
                                                SencHazard Hazard = new SencHazard()
                                                {
                                                    FRID = linker.FRID,
                                                    DEPTH = float.MaxValue,
                                                };

                                                (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Vertex = Import_SencVertex(linker);

                                                if (Vertex.Point != null) { Hazard.Point = Vertex.Point; };
                                                if (Vertex.Edge != null) { Hazard.Edge = Vertex.Edge; };
                                                if (Vertex.Shape != null) { Hazard.Shape = Vertex.Shape; };

                                                chart.Hazard ??= new List<SencHazard>();
                                                chart.Hazard.Add(Hazard);
                                            }

                                            break;
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    break;
                case 4: // Hazard Depth
                    {
                        SencHazard Hazard = new SencHazard()
                        {
                            FRID = linker.FRID,
                        };

                        if (linker.FRID.OBJL == 86)
                        {
                            if (!linker.CS)
                            {
                                float VALSOU = float.MaxValue;
                                byte WATLEV = 255;

                                if (feature.ATTF != null)
                                {
                                    foreach (DCC.ATTF ATTF in feature.ATTF)
                                    {
                                        switch (ATTF.ATTL)
                                        {
                                            case 179:
                                                if ((ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] != "9999"))
                                                {
                                                    float.TryParse(ATTF.ATVL[0], out VALSOU);
                                                }
                                                break;
                                            case 187:
                                                if ((ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] != "9999"))
                                                {
                                                    byte.TryParse(ATTF.ATVL[0], out WATLEV);
                                                }
                                                break;
                                        }
                                    }
                                }

                                if ((VALSOU == float.MaxValue) && (WATLEV == 7)) { VALSOU = -15.0f; }

                                Hazard.DEPTH = VALSOU;
                            }
                            else
                            {
                                Hazard.DEPTH = linker.Danger_DEPTH;
                            }
                        }
                        else
                        {
                            Hazard.DEPTH = linker.Danger_DEPTH;
                        }

                        (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Vertex = Import_SencVertex(linker);

                        if (Vertex.Point != null) { Hazard.Point = Vertex.Point; };
                        if (Vertex.Edge != null) { Hazard.Edge = Vertex.Edge; };
                        if (Vertex.Shape != null) { Hazard.Shape = Vertex.Shape; };

                        chart.Hazard_Depth ??= new List<SencHazard>();
                        chart.Hazard_Depth.Add(Hazard);
                    }
                    break;
                case 5: // Hazard Sound
                    if (feature.ATTF != null)
                    {
                        foreach (DCC.ATTF ATTF in feature.ATTF)
                        {
                            if (ATTF.ATTL == 93)
                            {
                                if ((ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] != "9999") && int.TryParse(ATTF.ATVL[0], out int ATVL) && (ATVL == 2))
                                {
                                    SencHazard Hazard = new SencHazard()
                                    {
                                        FRID = linker.FRID,
                                        DEPTH = float.MaxValue,
                                    };

                                    if (linker.Sound != null)
                                    {
                                        foreach (DCC.Sound Sound in linker.Sound)
                                        {
                                            Hazard.SOUNDG ??= new List<SCE.SencHazardSound>();
                                            Hazard.SOUNDG.Add(new SCE.SencHazardSound() {
                                                Sound = Sound.Sounding,
                                                X = Sound.XCOO,
                                                Y = Sound.YCOO,
                                            });
                                        }
                                    }

                                    (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Vertex = Import_SencVertex(linker);

                                    if (Vertex.Point != null) { Hazard.Point = Vertex.Point; };
                                    if (Vertex.Edge != null) { Hazard.Edge = Vertex.Edge; };
                                    if (Vertex.Shape != null) { Hazard.Shape = Vertex.Shape; };

                                    chart.Hazard_Sound ??= new List<SencHazard>();
                                    chart.Hazard_Sound.Add(Hazard);
                                }

                                break;
                            }
                        }
                    }
                    break;
            }
        }

        private (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Import_SencVertex(DCC.FeatureLinker linker)
        {
            (List<SCE.SencPoint>? Point, List<SCE.SencEdge>? Edge, List<SCE.SencShape>? Shape) Result = (null, null, null);

            if (linker.FRID.PRIM == 1)
            {
                Result.Point = new List<SCE.SencPoint>() {
                    new SCE.SencPoint() {
                        X = linker.Pivot.X,
                        Y = linker.Pivot.Y,
                    },
                };

                Result.Shape = new List<SCE.SencShape>() {
                    new SCE.SencShape() {
                        Edge = 0,
                        Point = (linker.Shape?.Count > 0) ? linker.Shape[0].Vector_2D.SG2D?.Count ?? 0 : 0,
                    },
                };
            }
            else
            {
                if (linker.Shape != null)
                {
                    int Start = 0;

                    foreach (DCC.ShapeLinker Shape_Linker in linker.Shape)
                    {
                        SCE.SencShape Senc_Shape = new SCE.SencShape();

                        if (Shape_Linker.Edge != null)
                        {
                            Senc_Shape.Edge = Shape_Linker.Edge.Count;

                            for (int i = 0; i < Shape_Linker.Edge.Count; i++)
                            {
                                DCC.EdgeLinker Edge_Linker = Shape_Linker.Edge[i];

                                SCE.SencEdge Senc_Edge = new SCE.SencEdge()
                                {
                                    Count = Edge_Linker.SG2D?.Count ?? 0,
                                    Mask = Edge_Linker.MASK,
                                    QUAPOS = (int)Edge_Linker.ATVL,
                                    Start = Start,
                                };

                                if (linker.FRID.PRIM == 3)
                                {
                                    if (i == (Shape_Linker.Edge.Count - 1))
                                    {
                                        Start += Senc_Edge.Count;
                                    }
                                    else
                                    {
                                        Start += Senc_Edge.Count - 1;
                                    }
                                }
                                else
                                {

                                    Start += Senc_Edge.Count;
                                }

                                Result.Edge ??= new List<SCE.SencEdge>();
                                Result.Edge.Add(Senc_Edge);
                            }
                        }

                        if (Shape_Linker.Point != null)
                        {
                            Senc_Shape.Point = Shape_Linker.Point.Count;

                            foreach (DCC.SG2D SG2D in Shape_Linker.Point)
                            {
                                Result.Point ??= new List<SCE.SencPoint>();
                                Result.Point.Add(new SCE.SencPoint() {
                                    X = SG2D.XCOO,
                                    Y = SG2D.YCOO,
                                });
                            }
                        }

                        Result.Shape ??= new List<SCE.SencShape>();
                        Result.Shape.Add(Senc_Shape);
                    }
                }
            }

            return Result;
        }


        internal void Serialize_SENC(SencChart chart)
        {
            DirectoryInfo SENC_DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, Using_Chart1 ? "SENC1" : "SENC"));
            FileInfo SENC_FileInfo = new FileInfo(Path.Combine(SENC_DirectoryInfo.FullName, $"{chart.Name}.enc"));

            if (!SENC_DirectoryInfo.Exists) { SENC_DirectoryInfo.Create(); }
            if (SENC_FileInfo.Exists) { SENC_FileInfo.Delete(); }

            using (FileStream SENC_Stream = new FileStream(SENC_FileInfo.FullName, FileMode.CreateNew, FileAccess.Write))
            using (BinaryWriter SENC_Writer = new BinaryWriter(SENC_Stream))
            {
                int[] digits = DateTime.Now.ToString("yyyyMMdd").Select(c => c - '0').ToArray();

                foreach (int digit in digits)
                {
                    SENC_Writer.Write(digit);
                }

                switch (chart.Name)
                {
                    case string _ when chart.Name.Contains("KRINDEX1"):
                    case string _ when chart.Name.Contains("KRINDEX2"):
                    case string _ when chart.Name.Contains("KRINDEX3"):
                    case string _ when chart.Name.Contains("KRINDEX4"):
                    case string _ when chart.Name.Contains("KRINDEX5"):
                    case string _ when chart.Name.Contains("KRINDEX6"):
                        {
                            Serialize_IndexObjectSize(SENC_Writer, chart);

                            Serialize_LNDARE(SENC_Writer, chart);
                        }
                        break;
                    default:
                        {
                            Serialize_ObjectSize(SENC_Writer, chart);

                            Serialize_DEPARE(SENC_Writer, chart);
                            Serialize_LNDARE(SENC_Writer, chart);
                            Serialize_DRGARE(SENC_Writer, chart);
                            Serialize_UNSARE(SENC_Writer, chart);
                            Serialize_DEPCNT(SENC_Writer, chart);
                            Serialize_OBSTRN(SENC_Writer, chart);
                            Serialize_WRECKS(SENC_Writer, chart);
                            Serialize_LIGHTS(SENC_Writer, chart);
                            Serialize_SOUNDG(SENC_Writer, chart);
                            Serialize_SLCONS(SENC_Writer, chart);
                            Serialize_Meta(SENC_Writer, chart);
                            Serialize_OBJECT(SENC_Writer, chart);

                            SENC_Writer.Write(chart.Not_UpToDate);
                        }
                        break;
                }
            }
        }

        public void Serialize_IndexObjectSize(BinaryWriter writer, SencChart chart)
        {
            if (chart.LNDARE != null)
            {
                writer.Write(chart.LNDARE.Count);


                uint Index = 0;

                foreach (SencLndare LNDARE in chart.LNDARE)
                {
                    writer.Write(LNDARE.Display_Group);
                    writer.Write(OBJL.LNDARE);
                    writer.Write(Index);

                    Index += Get_Size(LNDARE);
                }
            }
            else
            {
                writer.Write(0);
            }
        }

        public void Serialize_ObjectSize(BinaryWriter writer, SencChart chart)
        {
            uint Object_Count = 0;

            if (chart.DEPARE?.Count > 0) { Object_Count += (uint)chart.DEPARE.Count; }
            if (chart.LNDARE?.Count > 0) { Object_Count += (uint)chart.LNDARE.Count; }
            if (chart.DRGARE?.Count > 0) { Object_Count += (uint)chart.DRGARE.Count; }
            if (chart.UNSARE?.Count > 0) { Object_Count += (uint)chart.UNSARE.Count; }
            if (chart.DEPCNT?.Count > 0) { Object_Count += (uint)chart.DEPCNT.Count; }
            if (chart.OBSTRN_A?.Count > 0) { Object_Count += (uint)chart.OBSTRN_A.Count; }
            if (chart.OBSTRN_L?.Count > 0) { Object_Count += (uint)chart.OBSTRN_L.Count; }
            if (chart.OBSTRN_P?.Count > 0) { Object_Count += (uint)chart.OBSTRN_P.Count; }
            if (chart.WRECKS_A?.Count > 0) { Object_Count += (uint)chart.WRECKS_A.Count; }
            if (chart.WRECKS_P?.Count > 0) { Object_Count += (uint)chart.WRECKS_P.Count; }
            if (chart.LIGHTS?.Count > 0) { Object_Count += (uint)chart.LIGHTS.Count; }
            if (chart.SOUNDG?.Count > 0) { Object_Count += (uint)chart.SOUNDG.Count; }
            if (chart.SLCONS_A?.Count > 0) { Object_Count += (uint)chart.SLCONS_A.Count; }
            if (chart.SLCONS_L?.Count > 0) { Object_Count += (uint)chart.SLCONS_L.Count; }
            if (chart.SLCONS_P?.Count > 0) { Object_Count += (uint)chart.SLCONS_P.Count; }
            if (chart.Meta?.Count > 0) { Object_Count += (uint)chart.Meta.Count; }
            if (chart.OBJECT_A?.Count > 0) { Object_Count += (uint)chart.OBJECT_A.Count; }
            if (chart.OBJECT_L?.Count > 0) { Object_Count += (uint)chart.OBJECT_L.Count; }
            if (chart.OBJECT_P?.Count > 0) { Object_Count += (uint)chart.OBJECT_P.Count; }

            writer.Write(Object_Count);


            if (Object_Count > 0)
            {
                uint Index = 0;

                if (chart.DEPARE?.Count > 0)
                {
                    foreach (SencDepare DEPARE in chart.DEPARE)
                    {
                        writer.Write(DEPARE.Display_Group);
                        writer.Write(OBJL.DEPARE);
                        writer.Write(Index);

                        Index += Get_Size(DEPARE);
                    }
                }

                if (chart.LNDARE?.Count > 0)
                {
                    foreach (SencLndare LNDARE in chart.LNDARE)
                    {
                        writer.Write(LNDARE.Display_Group);
                        writer.Write(OBJL.LNDARE);
                        writer.Write(Index);

                        Index += Get_Size(LNDARE);
                    }
                }

                if (chart.DRGARE?.Count > 0)
                {
                    foreach (SencDrgare DRGARE in chart.DRGARE)
                    {
                        writer.Write(DRGARE.Display_Group);
                        writer.Write(OBJL.DRGARE);
                        writer.Write(Index);

                        Index += Get_Size(DRGARE);
                    }
                }

                if (chart.UNSARE?.Count > 0)
                {
                    foreach (SencUnsare UNSARE in chart.UNSARE)
                    {
                        writer.Write(UNSARE.Display_Group);
                        writer.Write(OBJL.UNSARE);
                        writer.Write(Index);

                        Index += Get_Size(UNSARE);
                    }
                }

                if (chart.DEPCNT?.Count > 0)
                {
                    foreach (SencDepcnt DEPCNT in chart.DEPCNT)
                    {
                        writer.Write(DEPCNT.Display_Group);
                        writer.Write(OBJL.DEPCNT);
                        writer.Write(Index);

                        Index += Get_Size(DEPCNT);
                    }
                }

                if (chart.OBSTRN_A?.Count > 0)
                {
                    foreach (SencObstrn OBSTRN in chart.OBSTRN_A)
                    {
                        writer.Write(OBSTRN.Display_Group);
                        writer.Write(OBJL.OBSTRN);
                        writer.Write(Index);

                        Index += Get_Size(OBSTRN);
                    }
                }

                if (chart.OBSTRN_L?.Count > 0)
                {
                    foreach (SencObstrn OBSTRN in chart.OBSTRN_L)
                    {
                        writer.Write(OBSTRN.Display_Group);
                        writer.Write(OBJL.OBSTRN);
                        writer.Write(Index);

                        Index += Get_Size(OBSTRN);
                    }
                }

                if (chart.OBSTRN_P?.Count > 0)
                {
                    foreach (SencObstrn OBSTRN in chart.OBSTRN_P)
                    {
                        writer.Write(OBSTRN.Display_Group);
                        writer.Write(OBJL.OBSTRN);
                        writer.Write(Index);

                        Index += Get_Size(OBSTRN);
                    }
                }

                if (chart.WRECKS_A?.Count > 0)
                {
                    foreach (SencObstrn WRECKS in chart.WRECKS_A)
                    {
                        writer.Write(WRECKS.Display_Group);
                        writer.Write(OBJL.WRECKS);
                        writer.Write(Index);

                        Index += Get_Size(WRECKS);
                    }
                }

                if (chart.WRECKS_P?.Count > 0)
                {
                    foreach (SencObstrn WRECKS in chart.WRECKS_P)
                    {
                        writer.Write(WRECKS.Display_Group);
                        writer.Write(OBJL.WRECKS);
                        writer.Write(Index);

                        Index += Get_Size(WRECKS);
                    }
                }

                if (chart.LIGHTS?.Count > 0)
                {
                    foreach (SencLights LIGHTS in chart.LIGHTS)
                    {
                        writer.Write(LIGHTS.Display_Group);
                        writer.Write(OBJL.LIGHTS);
                        writer.Write(Index);

                        Index += Get_Size(LIGHTS);
                    }
                }

                if (chart.SOUNDG?.Count > 0)
                {
                    foreach (SencSoundg SOUNDG in chart.SOUNDG)
                    {
                        writer.Write(SOUNDG.Display_Group);
                        writer.Write(OBJL.SOUNDG);
                        writer.Write(Index);

                        Index += Get_Size(SOUNDG);
                    }
                }

                if (chart.SLCONS_A?.Count > 0)
                {
                    foreach (SencSlcons SLCONS in chart.SLCONS_A)
                    {
                        writer.Write(SLCONS.Display_Group);
                        writer.Write(OBJL.SLCONS);
                        writer.Write(Index);

                        Index += Get_Size(SLCONS);
                    }
                }

                if (chart.SLCONS_L?.Count > 0)
                {
                    foreach (SencSlcons SLCONS in chart.SLCONS_L)
                    {
                        writer.Write(SLCONS.Display_Group);
                        writer.Write(OBJL.SLCONS);
                        writer.Write(Index);

                        Index += Get_Size(SLCONS);
                    }
                }

                if (chart.SLCONS_P?.Count > 0)
                {
                    foreach (SencSlcons SLCONS in chart.SLCONS_P)
                    {
                        writer.Write(SLCONS.Display_Group);
                        writer.Write(OBJL.SLCONS);
                        writer.Write(Index);

                        Index += Get_Size(SLCONS);
                    }
                }

                if (chart.Meta?.Count > 0)
                {
                    foreach (SencMeta Meta in chart.Meta)
                    {
                        writer.Write(Meta.Display_Group);
                        writer.Write(Meta.FRID.OBJL);
                        writer.Write(Index);

                        Index += Get_Size(Meta);
                    }
                }

                if (chart.OBJECT_A?.Count > 0)
                {
                    foreach (SencObject OBJECT in chart.OBJECT_A)
                    {
                        writer.Write(OBJECT.Display_Group);
                        writer.Write(OBJECT.OBJL);
                        writer.Write(Index);

                        Index += Get_Size(OBJECT);
                    }
                }

                if (chart.OBJECT_L?.Count > 0)
                {
                    foreach (SencObject OBJECT in chart.OBJECT_L)
                    {
                        writer.Write(OBJECT.Display_Group);
                        writer.Write(OBJECT.OBJL);
                        writer.Write(Index);
                        
                        Index += Get_Size(OBJECT);
                    }
                }

                if (chart.OBJECT_P?.Count > 0)
                {
                    foreach (SencObject OBJECT in chart.OBJECT_P)
                    {
                        writer.Write(OBJECT.Display_Group);
                        writer.Write(OBJECT.OBJL);
                        writer.Write(Index);

                        Index += Get_Size(OBJECT);
                    }
                }
            }
        }

        public void Serialize_DEPARE(BinaryWriter writer, SencChart chart)
        {
            if (chart.DEPARE?.Count > 0)
            {
                foreach (SencDepare DEPARE in chart.DEPARE)
                {
                    writer.Write(DEPARE.Radar_Overlay);
                    writer.Write(DEPARE.RCID);
                    writer.Write(DEPARE.DRVAL1);
                    writer.Write(DEPARE.DRVAL2);
                    writer.Write(DEPARE.Update_Type);

                    Serialize_Vertex(writer, DEPARE.Point, DEPARE.Edge, DEPARE.Shape);
                    Serialize_Command(writer, DEPARE.Command);

                    if (DEPARE.Edge_Attribute?.Count > 0)
                    {
                        foreach (DCC.EdgeAttribute Edge_Attribute in DEPARE.Edge_Attribute)
                        {
                            writer.Write(Edge_Attribute.UNSAFE);
                            writer.Write(Edge_Attribute.VALDCO);
                            writer.Write(Edge_Attribute.DRVAL1);
                        }
                    }
                }
            }
        }

        public void Serialize_LNDARE(BinaryWriter writer, SencChart chart)
        {
            if (chart.LNDARE?.Count > 0)
            {
                foreach (SencLndare LNDARE in chart.LNDARE)
                {
                    writer.Write(LNDARE.Radar_Overlay);
                    writer.Write(LNDARE.RCID);
                    writer.Write(LNDARE.PRIM);
                    writer.Write(LNDARE.Pivot.X);
                    writer.Write(LNDARE.Pivot.Y);
                    writer.Write(LNDARE.Minimum_Scale);
                    writer.Write(LNDARE.Information);
                    writer.Write(LNDARE.Update_Type);

                    if (LNDARE.PRIM != 1) { Serialize_Vertex(writer, LNDARE.Point, LNDARE.Edge, LNDARE.Shape); }
                    Serialize_Command(writer, LNDARE.Command);
                }
            }
        }

        public void Serialize_DRGARE(BinaryWriter writer, SencChart chart)
        {
            if (chart.DRGARE?.Count > 0)
            {
                foreach (SencDrgare DRGARE in chart.DRGARE)
                {
                    writer.Write(DRGARE.Radar_Overlay);
                    writer.Write(DRGARE.RCID);
                    writer.Write(DRGARE.DRVAL1);
                    writer.Write(DRGARE.Information);
                    writer.Write(DRGARE.Update_Type);

                    Serialize_Vertex(writer, DRGARE.Point, DRGARE.Edge, DRGARE.Shape);
                    Serialize_Command(writer, DRGARE.Command);

                    if (DRGARE.Edge_Attribute?.Count > 0)
                    {
                        foreach (DCC.EdgeAttribute Edge_Attribute in DRGARE.Edge_Attribute)
                        {
                            writer.Write(Edge_Attribute.UNSAFE);
                            writer.Write(Edge_Attribute.VALDCO);
                            writer.Write(Edge_Attribute.DRVAL1);
                        }
                    }
                }
            }
        }

        public void Serialize_UNSARE(BinaryWriter writer, SencChart chart)
        {
            if (chart.UNSARE?.Count > 0)
            {
                foreach (SencUnsare UNSARE in chart.UNSARE)
                {
                    writer.Write(UNSARE.Radar_Overlay);
                    writer.Write(UNSARE.RCID);
                    writer.Write(UNSARE.Update_Type);

                    Serialize_Vertex(writer, UNSARE.Point, UNSARE.Edge, UNSARE.Shape);
                    Serialize_Command(writer, UNSARE.Command);
                }
            }
        }

        public void Serialize_DEPCNT(BinaryWriter writer, SencChart chart)
        {
            if (chart.DEPCNT?.Count > 0)
            {
                foreach (SencDepcnt DEPCNT in chart.DEPCNT)
                {
                    writer.Write(DEPCNT.Radar_Overlay);
                    writer.Write(DEPCNT.RCID);
                    writer.Write(DEPCNT.Minimum_Scale);
                    writer.Write(DEPCNT.VALDCO);
                    writer.Write(DEPCNT.Update_Type);

                    Serialize_Vertex(writer, DEPCNT.Point, DEPCNT.Edge, DEPCNT.Shape);
                }
            }
        }

        public void Serialize_OBSTRN(BinaryWriter writer, SencChart chart)
        {
            if (chart.OBSTRN_A?.Count > 0)
            {
                foreach (SencObstrn OBSTRN in chart.OBSTRN_A)
                {
                    writer.Write(OBSTRN.Radar_Overlay);
                    writer.Write(OBSTRN.RCID);
                    writer.Write(OBSTRN.PRIM);
                    writer.Write(OBSTRN.OBJL);
                    writer.Write(OBSTRN.Pivot.X);
                    writer.Write(OBSTRN.Pivot.Y);
                    writer.Write(OBSTRN.Minimum_Scale);
                    writer.Write(OBSTRN.Information);
                    writer.Write(OBSTRN.Viewing_Group);
                    writer.Write(OBSTRN.Update_Type);

                    Serialize_Vertex(writer, OBSTRN.Point, OBSTRN.Edge, OBSTRN.Shape);
                    Serialize_Command(writer, OBSTRN.Command);
                    Serialize_DangerAttribute(writer, OBSTRN);
                }
            }

            if (chart.OBSTRN_L?.Count > 0)
            {
                foreach (SencObstrn OBSTRN in chart.OBSTRN_L)
                {
                    writer.Write(OBSTRN.Radar_Overlay);
                    writer.Write(OBSTRN.RCID);
                    writer.Write(OBSTRN.PRIM);
                    writer.Write(OBSTRN.OBJL);
                    writer.Write(OBSTRN.Pivot.X);
                    writer.Write(OBSTRN.Pivot.Y);
                    writer.Write(OBSTRN.Minimum_Scale);
                    writer.Write(OBSTRN.Information);
                    writer.Write(OBSTRN.Viewing_Group);
                    writer.Write(OBSTRN.Update_Type);

                    Serialize_Vertex(writer, OBSTRN.Point, OBSTRN.Edge, OBSTRN.Shape);
                    Serialize_Command(writer, OBSTRN.Command);
                    Serialize_DangerAttribute(writer, OBSTRN);
                }
            }

            if (chart.OBSTRN_P?.Count > 0)
            {
                foreach (SencObstrn OBSTRN in chart.OBSTRN_P)
                {
                    writer.Write(OBSTRN.Radar_Overlay);
                    writer.Write(OBSTRN.RCID);
                    writer.Write(OBSTRN.PRIM);
                    writer.Write(OBSTRN.OBJL);
                    writer.Write(OBSTRN.Pivot.X);
                    writer.Write(OBSTRN.Pivot.Y);
                    writer.Write(OBSTRN.Minimum_Scale);
                    writer.Write(OBSTRN.Information);
                    writer.Write(OBSTRN.Viewing_Group);
                    writer.Write(OBSTRN.Update_Type);

                    Serialize_Command(writer, OBSTRN.Command);
                    Serialize_DangerAttribute(writer, OBSTRN);
                }
            }
        }

        public void Serialize_WRECKS(BinaryWriter writer, SencChart chart)
        {
            if (chart.WRECKS_A?.Count > 0)
            {
                foreach (SencObstrn WRECKS in chart.WRECKS_A)
                {
                    writer.Write(WRECKS.Radar_Overlay);
                    writer.Write(WRECKS.RCID);
                    writer.Write(WRECKS.PRIM);
                    writer.Write(WRECKS.OBJL);
                    writer.Write(WRECKS.Pivot.X);
                    writer.Write(WRECKS.Pivot.Y);
                    writer.Write(WRECKS.Minimum_Scale);
                    writer.Write(WRECKS.Information);
                    writer.Write(WRECKS.Viewing_Group);
                    writer.Write(WRECKS.Update_Type);

                    Serialize_Vertex(writer, WRECKS.Point, WRECKS.Edge, WRECKS.Shape);
                    Serialize_Command(writer, WRECKS.Command);
                    Serialize_DangerAttribute(writer, WRECKS);
                }
            }

            if (chart.WRECKS_P?.Count > 0)
            {
                foreach (SencObstrn WRECKS in chart.WRECKS_P)
                {
                    writer.Write(WRECKS.Radar_Overlay);
                    writer.Write(WRECKS.RCID);
                    writer.Write(WRECKS.PRIM);
                    writer.Write(WRECKS.OBJL);
                    writer.Write(WRECKS.Pivot.X);
                    writer.Write(WRECKS.Pivot.Y);
                    writer.Write(WRECKS.Minimum_Scale);
                    writer.Write(WRECKS.Information);
                    writer.Write(WRECKS.Viewing_Group);
                    writer.Write(WRECKS.Update_Type);

                    Serialize_Command(writer, WRECKS.Command);
                    Serialize_DangerAttribute(writer, WRECKS);
                }
            }
        }

        public void Serialize_LIGHTS(BinaryWriter writer, SencChart chart)
        {
            if (chart.LIGHTS?.Count > 0)
            {
                foreach (SencLights LIGHTS in chart.LIGHTS)
                {
                    writer.Write(LIGHTS.Radar_Overlay);
                    writer.Write(LIGHTS.RCID);
                    writer.Write(LIGHTS.Pivot.X);
                    writer.Write(LIGHTS.Pivot.Y);
                    writer.Write(LIGHTS.Minimum_Scale);
                    writer.Write(LIGHTS.Information);
                    writer.Write(LIGHTS.Update_Type);

                    writer.Write(LIGHTS.All_Round_Light);
                    writer.Write(LIGHTS.CATLIT_1_16);
                    writer.Write(LIGHTS.CATLIT_8_11);
                    writer.Write(LIGHTS.CATLIT_9);
                    writer.Write(LIGHTS.LITVIS_3_7_8);
                    writer.Write(LIGHTS.COLOUR);
                    writer.Write(LIGHTS.Extended_Arc_Radius);
                    writer.Write(LIGHTS.Flare_At_45_Degrees);
                    writer.Write(LIGHTS.ORIENT);
                    writer.Write(LIGHTS.VALNMR);
                    writer.Write(LIGHTS.SECTR1);
                    writer.Write(LIGHTS.SECTR2);
                    writer.Write(LIGHTS.Radius_26mm);

                    if (!string.IsNullOrEmpty(LIGHTS.LITDSN))
                    {
                        byte[] LITDSN = Encoding.Default.GetBytes(LIGHTS.LITDSN);

                        writer.Write(LITDSN.Length);
                        if (LITDSN.Length > 0) { writer.Write(LITDSN); }
                    }
                    else
                    {
                        writer.Write(0);
                    }

                    writer.Write((byte)'\0'); // 이전 ECDIS에서 1Byte를 더 읽는 부분이 있어서 추가함
                }
            }
        }

        public void Serialize_SOUNDG(BinaryWriter writer, SencChart chart)
        {
            if (chart.SOUNDG?.Count > 0)
            {
                byte O_Sign = (byte)'o';
                byte Null_Sign = (byte)'\0';

                foreach (SencSoundg SOUNDG in chart.SOUNDG)
                {
                    writer.Write(SOUNDG.Radar_Overlay);
                    writer.Write(SOUNDG.RCID);
                    writer.Write(SOUNDG.Minimum_Scale);
                    writer.Write(SOUNDG.Update_Type);

                    if (SOUNDG.Sound?.Count > 0)
                    {
                        writer.Write(SOUNDG.Sound.Count);

                        foreach (DCC.Sound Sound in SOUNDG.Sound)
                        {
                            writer.Write(Sound.XCOO);
                            writer.Write(Sound.YCOO);
                            writer.Write(Sound.Sounding);

                            if (!string.IsNullOrEmpty(Sound.Sounding_Symbol))
                            {
                                byte[] Symbol = Encoding.Default.GetBytes(Sound.Sounding_Symbol);

                                if (Symbol.Length == 2)
                                {
                                    writer.Write(Symbol);
                                    writer.Write(Null_Sign);
                                    writer.Write(Null_Sign);
                                    writer.Write(Null_Sign);
                                }
                                else if (Symbol.Length == 4)
                                {
                                    writer.Write(Symbol);
                                    writer.Write(Null_Sign);
                                }
                                else
                                {
                                    writer.Write(O_Sign);
                                    writer.Write(O_Sign);
                                    writer.Write(O_Sign);
                                    writer.Write(O_Sign);
                                    writer.Write(Null_Sign);
                                }
                            }
                            else
                            {
                                writer.Write(O_Sign);
                                writer.Write(O_Sign);
                                writer.Write(O_Sign);
                                writer.Write(O_Sign);
                                writer.Write(Null_Sign);
                            }
                        }
                    }
                    else
                    {
                        writer.Write(0);
                    }
                }
            }
        }

        public void Serialize_SLCONS(BinaryWriter writer, SencChart chart)
        {
            if (chart.SLCONS_A?.Count > 0)
            {
                foreach (SencSlcons SLCONS in chart.SLCONS_A)
                {
                    writer.Write(SLCONS.Radar_Overlay);
                    writer.Write(SLCONS.RCID);
                    writer.Write(SLCONS.PRIM);
                    writer.Write(SLCONS.Pivot.X);
                    writer.Write(SLCONS.Pivot.Y);
                    writer.Write(SLCONS.Minimum_Scale);
                    writer.Write(SLCONS.Information);
                    writer.Write(SLCONS.Update_Type);

                    Serialize_Vertex(writer, SLCONS.Point, SLCONS.Edge, SLCONS.Shape);
                    Serialize_Command(writer, SLCONS.Command);
                    Serialize_EdgeCommand(writer, SLCONS.Edge_Command);
                }
            }

            if (chart.SLCONS_L?.Count > 0)
            {
                foreach (SencSlcons SLCONS in chart.SLCONS_L)
                {
                    writer.Write(SLCONS.Radar_Overlay);
                    writer.Write(SLCONS.RCID);
                    writer.Write(SLCONS.PRIM);
                    // 나중에 반드시 풀어야 함?
                    writer.Write(SLCONS.Pivot.X);
                    writer.Write(SLCONS.Pivot.Y);
                    writer.Write(SLCONS.Minimum_Scale);
                    writer.Write(SLCONS.Information);
                    writer.Write(SLCONS.Update_Type);

                    Serialize_Vertex(writer, SLCONS.Point, SLCONS.Edge, SLCONS.Shape);
                    Serialize_Command(writer, SLCONS.Command);
                    Serialize_EdgeCommand(writer, SLCONS.Edge_Command);
                }
            }

            if (chart.SLCONS_P?.Count > 0)
            {
                foreach (SencSlcons SLCONS in chart.SLCONS_P)
                {
                    writer.Write(SLCONS.Radar_Overlay);
                    writer.Write(SLCONS.RCID);
                    writer.Write(SLCONS.PRIM);
                    writer.Write(SLCONS.Pivot.X);
                    writer.Write(SLCONS.Pivot.Y);
                    writer.Write(SLCONS.Minimum_Scale);
                    writer.Write(SLCONS.Information);
                    writer.Write(SLCONS.Update_Type);

                    Serialize_Command(writer, SLCONS.Command);
                    Serialize_EdgeCommand(writer, SLCONS.Edge_Command);
                }
            }
        }

        public void Serialize_Meta(BinaryWriter writer, SencChart chart)
        {
            if (chart.Meta?.Count > 0)
            {
                foreach (SencMeta Meta in chart.Meta)
                {
                    writer.Write(Meta.Radar_Overlay);
                    writer.Write(Meta.FRID.RCID);
                    writer.Write(Meta.FRID.PRIM);
                    writer.Write(Meta.FRID.OBJL);
                    writer.Write(Meta.Viewing_Group);
                    writer.Write(Meta.Low_Accuracy);
                    writer.Write(Meta.Highlight);
                    writer.Write(Meta.CSCALE);
                    writer.Write(Meta.Update_Type);

                    Serialize_Vertex(writer, Meta.Point, Meta.Edge, Meta.Shape);
                    Serialize_Command(writer, Meta.Command);
                }
            }
        }

        public void Serialize_OBJECT(BinaryWriter writer, SencChart chart)
        {
            if (chart.OBJECT_A?.Count > 0)
            {
                foreach (SencObject OBJECT in chart.OBJECT_A)
                {
                    writer.Write(OBJECT.Radar_Overlay);
                    writer.Write(OBJECT.RCID);
                    writer.Write(OBJECT.PRIM);
                    writer.Write(OBJECT.OBJL);
                    writer.Write(OBJECT.Pivot.X);
                    writer.Write(OBJECT.Pivot.Y);
                    writer.Write(OBJECT.Valid_Date.Start);
                    writer.Write(OBJECT.Valid_Date.End);
                    writer.Write((byte)0);
                    writer.Write(OBJECT.Update_Type);
                    writer.Write(OBJECT.Group_Layer);
                    writer.Write(OBJECT.Minimum_Scale);
                    writer.Write(OBJECT.Information);
                    writer.Write(OBJECT.Reverse);

                    Serialize_Vertex(writer, OBJECT.Point, OBJECT.Edge, OBJECT.Shape);
                    Serialize_Command(writer, OBJECT.Command);
                    Serialize_EdgeMask(writer, OBJECT.Edge_Mask);
                }
            }

            if (chart.OBJECT_L?.Count > 0)
            {
                foreach (SencObject OBJECT in chart.OBJECT_L)
                {
                    writer.Write(OBJECT.Radar_Overlay);
                    writer.Write(OBJECT.RCID);
                    writer.Write(OBJECT.PRIM);
                    writer.Write(OBJECT.OBJL);
                    // 나중에?
                    writer.Write(OBJECT.Pivot.X);
                    writer.Write(OBJECT.Pivot.Y);
                    writer.Write(OBJECT.Valid_Date.Start);
                    writer.Write(OBJECT.Valid_Date.End);
                    writer.Write((byte)0);
                    writer.Write(OBJECT.Update_Type);
                    writer.Write(OBJECT.Group_Layer);
                    writer.Write(OBJECT.Minimum_Scale);
                    writer.Write(OBJECT.Information);
                    writer.Write(OBJECT.Reverse);

                    Serialize_Vertex(writer, OBJECT.Point, OBJECT.Edge, OBJECT.Shape);
                    Serialize_Command(writer, OBJECT.Command);
                    Serialize_EdgeMask(writer, OBJECT.Edge_Mask);
                }
            }

            if (chart.OBJECT_P?.Count > 0)
            {
                foreach (SencObject OBJECT in chart.OBJECT_P)
                {
                    writer.Write(OBJECT.Radar_Overlay);
                    writer.Write(OBJECT.RCID);
                    writer.Write(OBJECT.PRIM);
                    writer.Write(OBJECT.OBJL);
                    writer.Write(OBJECT.Pivot.X);
                    writer.Write(OBJECT.Pivot.Y);
                    writer.Write(OBJECT.Valid_Date.Start);
                    writer.Write(OBJECT.Valid_Date.End);
                    writer.Write((byte)0);
                    writer.Write(OBJECT.Update_Type);
                    writer.Write(OBJECT.Group_Layer);
                    writer.Write(OBJECT.Minimum_Scale);
                    writer.Write(OBJECT.Information);
                    writer.Write(OBJECT.Reverse);

                    Serialize_Command(writer, OBJECT.Command);
                }
            }
        }


        private void Serialize_Vertex(BinaryWriter writer, List<SCE.SencPoint>? point, List<SCE.SencEdge>? edge, List<SCE.SencShape>? shape)
        {
            writer.Write(point?.Count ?? 0);
            writer.Write(edge?.Count ?? 0);
            writer.Write(shape?.Count ?? 0);

            if (point?.Count > 0)
            {
                foreach (SCE.SencPoint Senc_Point in point)
                {
                    writer.Write(Senc_Point.X);
                    writer.Write(Senc_Point.Y);
                }
            }

            if (edge?.Count > 0)
            {
                foreach (SCE.SencEdge Senc_Edge in edge)
                {
                    writer.Write(Senc_Edge.Start);
                    writer.Write(Senc_Edge.Count);
                    writer.Write((byte)Senc_Edge.Mask);
                    writer.Write(Senc_Edge.QUAPOS > 0); // nQuapos : 1 = 퀄리티 좋음(true), 0 = 퀄리티 않좋음(false)
                    writer.Write(Senc_Edge.Reverse);
                }
            }

            if (shape?.Count > 0)
            {
                foreach (SCE.SencShape Senc_Shape in shape)
                {
                    writer.Write(Senc_Shape.Edge);
                    writer.Write(Senc_Shape.Point);
                }
            }
        }

        private void Serialize_Command(BinaryWriter writer, List<DCC.DrawCommand>? command)
        {
            writer.Write(command?.Count ?? 0);

            if (command?.Count > 0)
            {
                int SY_Size = 0;
                int LS_Size = 0;
                int LC_Size = 0;
                int AC_Size = 0;
                int AP_Size = 0;
                int TX_Size = 0;

                foreach (DCC.DrawCommand Draw_Command in command)
                {
                    int SY = Draw_Command.SY?.Count ?? 0;
                    int LS = Draw_Command.LS?.Count ?? 0;
                    int LC = Draw_Command.LC?.Count ?? 0;
                    int AC = Draw_Command.AC?.Count ?? 0;
                    int AP = Draw_Command.AP?.Count ?? 0;
                    int TX = Draw_Command.TX?.Count ?? 0;

                    writer.Write(SY);
                    writer.Write(LS);
                    writer.Write(LC);
                    writer.Write(AC);
                    writer.Write(AP);
                    writer.Write(TX);

                    SY_Size += SY;
                    LS_Size += LS;
                    LC_Size += LC;
                    AC_Size += AC;
                    AP_Size += AP;
                    TX_Size += TX;
                }

                writer.Write(SY_Size);
                writer.Write(LS_Size);
                writer.Write(LC_Size);
                writer.Write(AC_Size);
                writer.Write(AP_Size);
                writer.Write(TX_Size);


                if (SY_Size > 0)
                {
                    IEnumerable<DCC.SY> SY_Enumeration = command.Where(Command => Command.SY?.Count > 0).SelectMany(Command => Command.SY);

                    foreach (DCC.SY SY in SY_Enumeration)
                    {
                        writer.Write((short)SY.Index);
                        writer.Write(SY.Angle);
                    }
                }

                if (LS_Size > 0)
                {
                    IEnumerable<DCC.LS> LS_Enumeration = command.Where(Command => Command.LS?.Count > 0).SelectMany(Command => Command.LS);

                    foreach (DCC.LS LS in LS_Enumeration)
                    {
                        writer.Write((byte)LS.Pen_ColorIndex);
                        writer.Write((byte)LS.Pen_Type);
                        writer.Write((byte)LS.Pen_Width);
                    }
                }

                if (LC_Size > 0)
                {
                    IEnumerable<DCC.LC> LC_Enumeration = command.Where(Command => Command.LC?.Count > 0).SelectMany(Command => Command.LC);

                    foreach (DCC.LC LC in LC_Enumeration)
                    {
                        writer.Write((byte)LC.Index);
                    }
                }

                if (AC_Size > 0)
                {
                    IEnumerable<DCC.AC> AC_Enumeration = command.Where(Command => Command.AC?.Count > 0).SelectMany(Command => Command.AC);

                    foreach (DCC.AC AC in AC_Enumeration)
                    {
                        writer.Write((byte)AC.Index);
                        writer.Write((byte)AC.Trans); // trans를 -1을 기본값으로 만들어놓으면 여기서 -1을 byte 변환해서 입력이 되는데, 지금은 기본값을 0으로 설정해두어서 0으로 입력되는 차이였음
                    }
                }

                if (AP_Size > 0)
                {
                    IEnumerable<DCC.AP> AP_Enumeration = command.Where(Command => Command.AP?.Count > 0).SelectMany(Command => Command.AP);

                    foreach (DCC.AP AP in AP_Enumeration)
                    {
                        writer.Write((byte)AP.Index);
                    }
                }

                if (TX_Size > 0)
                {
                    IEnumerable<DCC.TX> TX_Enumeration = command.Where(Command => Command.TX?.Count > 0).SelectMany(Command => Command.TX);

                    foreach (DCC.TX TX in TX_Enumeration)
                    {
                        writer.Write(TX.Offset.X);
                        writer.Write(TX.Offset.Y);
                        writer.Write(TX.Align);
                        writer.Write(TX.Text_Group);
                        writer.Write(TX.Text_ColorIndex);

                        if (TX.Text?.Length > 0)
                        {
                            byte[] Text = Encoding.Default.GetBytes(TX.Text);

                            writer.Write(Text.Length);
                            writer.Write(Text);
                        }
                        else
                        {
                            writer.Write(0);
                        }

                        if (TX.NationalText?.Length > 0)
                        {
                            byte[] NationalText = Encoding.Default.GetBytes(TX.NationalText); // 나중에 반드시 풀어야 함? 무슨 말인지

                            writer.Write(NationalText.Length);
                            writer.Write(NationalText);
                        }
                        else
                        {
                            writer.Write(0);
                        }
                    }
                }
            }
        }

        private void Serialize_EdgeCommand(BinaryWriter writer, List<DCC.EdgeCommand>? edge_command)
        {
            if (edge_command?.Count > 0)
            {
                writer.Write(edge_command.Count);

                foreach (DCC.EdgeCommand Edge_Command in edge_command)
                {
                    bool SY = (Edge_Command.SY > -1);
                    bool LS = (Edge_Command.LS.Pen_ColorIndex > -1);
                    bool LC = (Edge_Command.LC > -1);

                    writer.Write(SY);
                    if (SY) { writer.Write((short)Edge_Command.SY); }

                    writer.Write(LS);
                    if (LS) {
                        writer.Write((byte)Edge_Command.LS.Pen_ColorIndex);
                        writer.Write((byte)Edge_Command.LS.Pen_Type);
                        writer.Write((byte)Edge_Command.LS.Pen_Width);
                    }

                    writer.Write(LC);
                    if (LC) { writer.Write((byte)Edge_Command.LC); }
                }
            }
            else
            {
                writer.Write(0);
            }
        }

        private void Serialize_EdgeMask(BinaryWriter writer, List<DCC.EdgeMask>? edge_mask)
        {
            if (edge_mask?.Count > 0)
            {
                writer.Write(edge_mask.Count);

                foreach (DCC.EdgeMask Edge_Mask in edge_mask)
                {
                    writer.Write(Edge_Mask.RCID);
                    writer.Write(Edge_Mask.Type);
                    writer.Write(Edge_Mask.Number);
                }
            }
            else
            {
                writer.Write(0);
            }
        }

        private void Serialize_DangerAttribute(BinaryWriter writer, SencObstrn obstrn)
        {
            writer.Write(obstrn.Danger_Accuracy);
            writer.Write(obstrn.Danger_WATLEV_1_2);
            writer.Write(obstrn.Danger_DEPTH);
            // 나중에 반드시 풀어야 함?
            writer.Write(obstrn.DRVAL1);
            writer.Write(obstrn.VALSOU);
            writer.Write(obstrn.Sounding);

            byte O_Sign = (byte)'o';
            byte Null_Sign = (byte)'\0';

            if (!string.IsNullOrEmpty(obstrn.Sounding_Symbol))
            {
                byte[] Symbol = Encoding.Default.GetBytes(obstrn.Sounding_Symbol);

                if (Symbol.Length == 2)
                {
                    writer.Write(Symbol);
                    writer.Write(Null_Sign);
                    writer.Write(Null_Sign);
                    writer.Write(Null_Sign);
                }
                else if (Symbol.Length == 4)
                {
                    writer.Write(Symbol);
                    writer.Write(Null_Sign);
                }
                else
                {
                    writer.Write(O_Sign);
                    writer.Write(O_Sign);
                    writer.Write(O_Sign);
                    writer.Write(O_Sign);
                    writer.Write(Null_Sign);
                }
            }
            else
            {
                writer.Write(O_Sign);
                writer.Write(O_Sign);
                writer.Write(O_Sign);
                writer.Write(O_Sign);
                writer.Write(Null_Sign);
            }
        }


        private uint Get_VertexSize(List<SCE.SencPoint>? point, List<SCE.SencEdge>? edge, List<SCE.SencShape>? shape)
        {
            uint Size = 12; // 12인 이유는 vecPT, vecEdge, vecShape의 Size가 먼저 int(4) * 3 들어가기 때문이다.

            if (point?.Count > 0) { Size += (uint)point.Count * 8; }
            if (edge?.Count > 0) { Size += (uint)edge.Count * 11; }
            if (shape?.Count > 0) { Size += (uint)shape.Count * 8; }

            return Size;
        }

        private uint Get_CommandSize(List<DCC.DrawCommand>? command)
        {
            uint Size = 4; // Size를 먼저 써준다.

            if (command?.Count > 0)
            {
                uint Unit_Size = sizeof(int) * 6;

                Size += Unit_Size * (uint)command.Count; // SY,TX,LS,LC,AC,AP에 대해서 Size를 vecCom의 개수만큼 적어놓는다.
                Size += Unit_Size; // SY,TX,LS,LC,AC,AP 각각에 대한 Total Size를 적어 놓는다.

                foreach (DCC.DrawCommand Draw_Command in command)
                {
                    if (Draw_Command.SY?.Count > 0) { Size += (uint)Draw_Command.SY.Count * 6; } // short + float
                    if (Draw_Command.LS?.Count > 0) { Size += (uint)Draw_Command.LS.Count * 3; } // byte + byte + byte
                    if (Draw_Command.LC?.Count > 0) { Size += (uint)Draw_Command.LC.Count; } // byte
                    if (Draw_Command.AC?.Count > 0) { Size += (uint)Draw_Command.AC.Count * 2; } // byte + byte
                    if (Draw_Command.AP?.Count > 0) { Size += (uint)Draw_Command.AP.Count; } // byte
                }

                IEnumerable<DCC.DrawCommand> Command_Enumeration = command.Where(Command => Command.TX?.Count > 0);

                foreach (DCC.DrawCommand Draw_Command in Command_Enumeration)
                {
                    if (Draw_Command.TX?.Count > 0)
                    {
                        foreach (DCC.TX TX in Draw_Command.TX)
                        {
                            Size += 19; // ST_COM_TX_ADD의 Size가 11이므로 11을 추가함 + 일반텍스트 길이(int) + National Text길이(int)
                            Size += (uint)((TX.Text?.Length > 0) ? Encoding.Default.GetBytes(TX.Text).Length : 0);
                            Size += (uint)((TX.NationalText?.Length > 0) ? Encoding.Default.GetBytes(TX.NationalText).Length : 0);
                        }
                    }
                }
            }

            return Size;
        }

        private uint Get_EdgeAttributeSize(List<DCC.EdgeAttribute>? edge_attribute)
        {
            return (edge_attribute?.Count > 0) ? (uint)edge_attribute.Count * 9 : 0; // 9인 이유는 bUNSAFE(1), fVALDCO(4), fDRVAL1(4)
        }

        private uint Get_EdgeCommandSize(List<DCC.EdgeCommand>? edge_command)
        {
            uint Size = 4;

            if (edge_command?.Count > 0)
            {
                foreach (DCC.EdgeCommand Edge_Command in edge_command)
                {
                    Size += 3;

                    if (Edge_Command.SY > -1) { Size += 2; }
                    if (Edge_Command.LS.Pen_ColorIndex > -1) { Size += 3; }
                    if (Edge_Command.LC > -1) { Size += 1; }
                }
            }

            return Size;
        }

        private uint Get_EdgeMaskSize(List<DCC.EdgeMask>? edge_mask)
        {
            uint Size = 4;

            if (edge_mask?.Count > 0)
            {
                Size += (uint)edge_mask.Count * 9;
            }

            return Size;
        }

        private uint Get_DangerAttributeSize()
        {
            // bAccuracy(1), bWATLEV_1_2(1), fDEPTH_VALUE(4), fDRVAL1(4), fVALSOU(4), bSound(1)
            // char(5)

            return 20;
        }

        private uint Get_LightAttributeSize(string? litdsn)
        {
            return (litdsn?.Length > 0) ? ((uint)litdsn.Length + 1) : 1;
        }

        private uint Get_SoundSize(List<DCC.Sound>? sound)
        {
            return (sound?.Count > 0) ? ((uint)sound.Count * 17) : 0;
        }

        private uint Get_Size(SencDepare depare)
        {
            uint Size = 14;

            Size += Get_VertexSize(depare.Point, depare.Edge, depare.Shape);
            Size += Get_CommandSize(depare.Command);
            Size += Get_EdgeAttributeSize(depare.Edge_Attribute);

            return Size;
        }
        
        private uint Get_Size(SencLndare lndare)
        {
            uint Size = 20;

            if (lndare.PRIM != 1) { Size += Get_VertexSize(lndare.Point, lndare.Edge, lndare.Shape); }
            Size += Get_CommandSize(lndare.Command);

            return Size;
        }

        private uint Get_Size(SencDrgare drgare)
        {
            uint Size = 11;

            Size += Get_VertexSize(drgare.Point, drgare.Edge, drgare.Shape);
            Size += Get_CommandSize(drgare.Command);
            Size += Get_EdgeAttributeSize(drgare.Edge_Attribute);

            return Size;
        }

        private uint Get_Size(SencUnsare unsare)
        {
            uint Size = 6;

            Size += Get_VertexSize(unsare.Point, unsare.Edge, unsare.Shape);
            Size += Get_CommandSize(unsare.Command);

            return Size;
        }

        private uint Get_Size(SencDepcnt depcnt)
        {
            uint Size = 14;

            Size += Get_VertexSize(depcnt.Point, depcnt.Edge, depcnt.Shape);

            return Size;
        }

        private uint Get_Size(SencObstrn obstrn)
        {
            uint Size = 23;

            if (obstrn.PRIM != 1) { Size += Get_VertexSize(obstrn.Point, obstrn.Edge, obstrn.Shape); }
            Size += Get_CommandSize(obstrn.Command);
            Size += Get_DangerAttributeSize();

            return Size;
        }

        private uint Get_Size(SencLights lights)
        {
            uint Size = 48;

            Size += Get_LightAttributeSize(lights.LITDSN);

            return Size;
        }

        private uint Get_Size(SencSoundg soundg)
        {
            uint Size = 14;

            Size += Get_SoundSize(soundg.Sound);

            return Size;
        }

        private uint Get_Size(SencSlcons slcons)
        {
            uint Size = 20;

            if (slcons.PRIM != 1) { Size += Get_VertexSize(slcons.Point, slcons.Edge, slcons.Shape); }
            Size += Get_CommandSize(slcons.Command);
            Size += Get_EdgeCommandSize(slcons.Edge_Command);

            return Size;
        }

        private uint Get_Size(SencMeta meta)
        {
            uint Size = 16;

            Size += Get_VertexSize(meta.Point, meta.Edge, meta.Shape);
            Size += Get_CommandSize(meta.Command);

            return Size;
        }

        private uint Get_Size(SencObject objt)
        {
            uint Size = 33;

            if (objt.PRIM != 1) { Size += Get_VertexSize(objt.Point, objt.Edge, objt.Shape); }
            Size += Get_CommandSize(objt.Command);
            if (objt.PRIM != 1) { Size += Get_EdgeMaskSize(objt.Edge_Mask); }

            return Size;
        }


        internal void Serialize_Search(SencChart chart, bool coverage = false)
        {
            DirectoryInfo Detection_DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "DETECTION"));
            DirectoryInfo Coverage_DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "COVERAGE"));
            FileInfo Detection_FileInfo = new FileInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "DETECTION", $"{chart.Name}.det"));
            FileInfo Coverage_FileInfo = new FileInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "COVERAGE", $"{chart.Name}.age"));

            if (!Detection_DirectoryInfo.Exists) { Detection_DirectoryInfo.Create(); }
            if (!Coverage_DirectoryInfo.Exists) { Coverage_DirectoryInfo.Create(); }
            if (Detection_FileInfo.Exists) { Detection_FileInfo.Delete(); }
            if (Coverage_FileInfo.Exists) { Coverage_FileInfo.Delete(); }

            if (!coverage)
            {
                using (FileStream Detection_Stream = new FileStream(Detection_FileInfo.FullName, FileMode.CreateNew, FileAccess.Write))
                using (BinaryWriter Detection_Writer = new BinaryWriter(Detection_Stream))
                {
                    Serialize_DetectionSize(Detection_Writer, chart);
                    Serialize_Detection(Detection_Writer, chart);
                }
            }

            using (FileStream Coverage_Stream = new FileStream(Coverage_FileInfo.FullName, FileMode.CreateNew, FileAccess.Write))
            using (BinaryWriter Coverage_Writer = new BinaryWriter(Coverage_Stream))
            {
                Serialize_CoverageSize(Coverage_Writer, chart);
                Serialize_Coverage(Coverage_Writer, chart);
            }
        }

        public void Serialize_DetectionSize(BinaryWriter writer, SencChart chart)
        {
            uint Detection_Count = 0;

            if (chart.Safety?.Count > 0) { Detection_Count += (uint)chart.Safety.Count; }
            if (chart.Safety_Depth?.Count > 0) { Detection_Count += (uint)chart.Safety_Depth.Count; }
            if (chart.Special?.Count > 0) { Detection_Count += (uint)chart.Special.Count; }
            if (chart.Hazard?.Count > 0) { Detection_Count += (uint)chart.Hazard.Count; }
            if (chart.Hazard_Depth?.Count > 0) { Detection_Count += (uint)chart.Hazard_Depth.Count; }
            if (chart.Hazard_Sound?.Count > 0) { Detection_Count += (uint)chart.Hazard_Sound.Count; }

            writer.Write(Detection_Count);


            if (Detection_Count > 0)
            {
                uint Index = 0;

                if (chart.Safety?.Count > 0)
                {
                    byte Safety_Detection = 0;

                    foreach (SencSafety Safety in chart.Safety)
                    {
                        writer.Write(Safety_Detection);
                        writer.Write(Index);

                        Index += Get_Size(Safety);
                    }
                }

                if (chart.Safety_Depth?.Count > 0)
                {
                    byte SafetyDepth_Detection = 1;

                    foreach (SencSafety Safety_Depth in chart.Safety_Depth)
                    {
                        writer.Write(SafetyDepth_Detection);
                        writer.Write(Index);

                        Index += Get_Size(Safety_Depth) + 4;
                    }
                }

                if (chart.Special?.Count > 0)
                {
                    byte Special_Detection = 2;

                    foreach (SencSpecial Special in chart.Special)
                    {
                        writer.Write(Special_Detection);
                        writer.Write(Index);

                        Index += Get_Size(Special);
                    }
                }

                if (chart.Hazard?.Count > 0)
                {
                    byte Hazard_Detection = 3;

                    foreach (SencHazard Hazard in chart.Hazard)
                    {
                        writer.Write(Hazard_Detection);
                        writer.Write(Index);

                        Index += Get_Size(Hazard);
                    }
                }

                if (chart.Hazard_Depth?.Count > 0)
                {
                    byte HazardDepth_Detection = 4;

                    foreach (SencHazard Hazard_Depth in chart.Hazard_Depth)
                    {
                        writer.Write(HazardDepth_Detection);
                        writer.Write(Index);

                        Index += Get_Size(Hazard_Depth) + 4;
                    }
                }

                if (chart.Hazard_Sound?.Count > 0)
                {
                    byte HazardSound_Detection = 5;

                    foreach (SencHazard Hazard_Sound in chart.Hazard_Sound)
                    {
                        writer.Write(HazardSound_Detection);
                        writer.Write(Index);

                        Index += Get_Size(Hazard_Sound) + 1;
                        if (Hazard_Sound.SOUNDG?.Count > 0) { Index += (uint)Hazard_Sound.SOUNDG.Count * 12; }
                    }
                }
            }
        }

        public void Serialize_Detection(BinaryWriter writer, SencChart chart)
        {
            if (chart.Safety?.Count > 0)
            {
                foreach (SencSafety Safety in chart.Safety)
                {
                    writer.Write(Safety.FRID.RCID);
                    writer.Write(Safety.FRID.OBJL);
                    writer.Write(Safety.FRID.PRIM);

                    Serialize_DetectionVertex(writer, Safety.Point, Safety.Shape, Safety.FRID.PRIM);
                }
            }

            if (chart.Safety_Depth?.Count > 0)
            {
                foreach (SencSafety Safety_Depth in chart.Safety_Depth)
                {
                    writer.Write(Safety_Depth.FRID.RCID);
                    writer.Write(Safety_Depth.FRID.OBJL);
                    writer.Write(Safety_Depth.FRID.PRIM);
                    writer.Write(Safety_Depth.DRVAL1);

                    Serialize_DetectionVertex(writer, Safety_Depth.Point, Safety_Depth.Shape, Safety_Depth.FRID.PRIM);
                }
            }

            if (chart.Special?.Count > 0)
            {
                foreach (SencSpecial Special in chart.Special)
                {
                    writer.Write(Special.FRID.RCID);
                    writer.Write(Special.FRID.OBJL);
                    writer.Write(Special.FRID.PRIM);
                    writer.Write(Special.RESARE);

                    Serialize_DetectionVertex(writer, Special.Point, Special.Shape, Special.FRID.PRIM);
                }
            }

            if (chart.Hazard?.Count > 0)
            {
                foreach (SencHazard Hazard in chart.Hazard)
                {
                    writer.Write(Hazard.FRID.RCID);
                    writer.Write(Hazard.FRID.OBJL);
                    writer.Write(Hazard.FRID.PRIM);

                    Serialize_DetectionVertex(writer, Hazard.Point, Hazard.Shape, Hazard.FRID.PRIM);
                }
            }

            if (chart.Hazard_Depth?.Count > 0)
            {
                foreach (SencHazard Hazard_Depth in chart.Hazard_Depth)
                {
                    writer.Write(Hazard_Depth.FRID.RCID);
                    writer.Write(Hazard_Depth.FRID.OBJL);
                    writer.Write(Hazard_Depth.FRID.PRIM);
                    writer.Write(Hazard_Depth.DEPTH);

                    Serialize_DetectionVertex(writer, Hazard_Depth.Point, Hazard_Depth.Shape, Hazard_Depth.FRID.PRIM);
                }
            }

            if (chart.Hazard_Sound?.Count > 0)
            {
                foreach (SencHazard Hazard_Sound in chart.Hazard_Sound)
                {
                    writer.Write(Hazard_Sound.FRID.RCID);

                    if (Hazard_Sound.SOUNDG?.Count > 0)
                    {
                        writer.Write(Hazard_Sound.SOUNDG.Count);

                        foreach (SCE.SencHazardSound SOUNDG in Hazard_Sound.SOUNDG)
                        {
                            writer.Write(SOUNDG.Sound);
                            writer.Write(SOUNDG.X);
                            writer.Write(SOUNDG.Y);
                        }
                    }
                    else
                    {
                        writer.Write(0);
                    }

                    Serialize_DetectionVertex(writer, Hazard_Sound.Point, Hazard_Sound.Shape, Hazard_Sound.FRID.PRIM);
                }
            }
        }

        public void Serialize_CoverageSize(BinaryWriter writer, SencChart chart)
        {
            if (chart.Cover?.Count > 0)
            {
                writer.Write(chart.Cover.Count);

                uint Index = 0;

                foreach (SencCover Cover in chart.Cover)
                {
                    writer.Write(Index);

                    Index += Get_Size(Cover);
                }
            }
            else
            {
                writer.Write(0);
            }
        }

        public void Serialize_Coverage(BinaryWriter writer, SencChart chart)
        {
            if (chart.Cover?.Count > 0)
            {
                foreach (SencCover Cover in chart.Cover)
                {
                    if (Cover.Point?.Count > 0)
                    {
                        writer.Write(Cover.Point.Count);
                        writer.Write(Cover.Cover2);

                        foreach (SCE.SencPoint Senc_Point in Cover.Point)
                        {
                            writer.Write(Senc_Point.X);
                            writer.Write(Senc_Point.Y);
                        }
                    }
                    else
                    {
                        writer.Write(0);
                        writer.Write(Cover.Cover2);
                    }
                }
            }
        }


        private void Serialize_DetectionVertex(BinaryWriter writer, List<SCE.SencPoint>? point, List<SCE.SencShape>? shape, byte prim)
        {
            if (point?.Count > 0)
            {
                writer.Write(point.Count);

                foreach (SCE.SencPoint Senc_Point in point)
                {
                    writer.Write(Senc_Point.X);
                    writer.Write(Senc_Point.Y);
                }
            }
            else
            {
                writer.Write(0);
            }

            if (prim == 3)
            {
                if (shape?.Count > 0)
                {
                    writer.Write(shape.Count);

                    foreach (SCE.SencShape Senc_Shape in shape)
                    {
                        writer.Write(Senc_Shape.Point);
                    }
                }
                else
                {
                    writer.Write(0);
                }
            }
        }


        private uint Get_DetectionVertexSize(List<SCE.SencPoint>? point, List<SCE.SencShape>? shape, byte prim)
        {
            uint Size = 4;

            if (point?.Count > 0) { Size += (uint)point.Count * 8; }

            if (prim == 3)
            {
                Size += 4;

                if (shape?.Count > 0) { Size += (uint)shape.Count * 4; }
            }

            return Size;
        }
        
        private uint Get_Size(SencSafety safety)
        {
            uint Size = 7;

            Size += Get_DetectionVertexSize(safety.Point, safety.Shape, safety.FRID.PRIM);

            return Size;
        }

        private uint Get_Size(SencSpecial special)
        {
            uint Size = 8;

            Size += Get_DetectionVertexSize(special.Point, special.Shape, special.FRID.PRIM);

            return Size;
        }

        private uint Get_Size(SencHazard hazard)
        {
            uint Size = 7;

            Size += Get_DetectionVertexSize(hazard.Point, hazard.Shape, hazard.FRID.PRIM);

            return Size;
        }

        private uint Get_Size(SencCover cover)
        {
            uint Size = 5;

            if (cover.Point?.Count > 0) { Size += (uint)cover.Point.Count * 8; }

            return Size;
        }


        internal void Serialize_Update(DetectionChart chart, bool index_chart)
        {
            DirectoryInfo Update_DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "UPDATE"));
            FileInfo Update_FileInfo = new FileInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "UPDATE", $"{chart.Name}.new"));

            if (!Update_DirectoryInfo.Exists) { Update_DirectoryInfo.Create(); }
            if (Update_FileInfo.Exists) { Update_FileInfo.Delete(); }

            if (!index_chart)
            {
                if (chart.Update_Record?.Count > 0)
                {
                    using (FileStream Update_Stream = new FileStream(Update_FileInfo.FullName, FileMode.CreateNew, FileAccess.Write))
                    using (BinaryWriter Update_Writer = new BinaryWriter(Update_Stream))
                    {
                        Update_Writer.Write(chart.Update_Record.Count);

                        foreach (DCC.UpdateRecord Update_Record in chart.Update_Record)
                        {
                            Update_Writer.Write(Update_Record.FRID.RCID);
                            Update_Writer.Write(Update_Record.FRID.PRIM);
                            Update_Writer.Write(Update_Record.VRID.RUIN);

                            if (Update_Record.SG2D?.Count > 0)
                            {
                                Update_Writer.Write(Update_Record.SG2D.Count);

                                foreach (DCC.SG2D SG2D in Update_Record.SG2D)
                                {
                                    Update_Writer.Write(SG2D.XCOO);
                                    Update_Writer.Write(SG2D.YCOO);
                                }
                            }
                            else
                            {
                                Update_Writer.Write(0);
                            }
                        }
                    }
                }
            }
        }
    }
}