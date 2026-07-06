using Legacy.ECM_Core.Chart;
using Legacy.ECM_Core.Definition;
using System.IO;

namespace Legacy.ECM_Core.Component
{
    public partial class ChartComposer
    {
        public DetectionChart? Detect_Chart(string chart_name)
        {
            DirectoryInfo Chart_DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.ENC_Directory, chart_name));

            if (Chart_DirectoryInfo.Exists)
            {
                FileInfo[] Cell_FileInfo = Chart_DirectoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly);
                List<(int UPDN, FileInfo File)> Cell_Collection = new List<(int UPDN, FileInfo File)>();

                foreach (FileInfo Cell in Cell_FileInfo)
                {
                    if (Cell.Name.StartsWith(chart_name) && int.TryParse(Cell.Extension.Replace(".", ""), out int UPDN))
                    {
                        Cell_Collection.Add((UPDN, Cell));
                    }
                }

                IEnumerable<(int UPDN, FileInfo File)> BaseCell_Enumeration = Cell_Collection.Where(Cell => Cell.UPDN == 0);

                if (BaseCell_Enumeration.Count() > 0)
                {
                    FileInfo BaseCell_FileInfo = BaseCell_Enumeration.First().File;
                    FileInfo Boundary_FileInfo = new FileInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.ENC_Directory, chart_name, "BOUNDARY"));

                    DetectionCell Base_Cell = new DetectionCell();

                    if (Boundary_FileInfo.Exists)
                    {
                        string[] Boundary = Boundary_FileInfo.OpenText().ReadToEnd().Split(',');

                        if (Boundary.Length > 3)
                        {
                            (double North, double South, double East, double West) Bound = (
                                double.TryParse(Boundary[0], out double North) ? North : 0.0,
                                double.TryParse(Boundary[1], out double South) ? South : 0.0,
                                double.TryParse(Boundary[2], out double East) ? East : 0.0,
                                double.TryParse(Boundary[3], out double West) ? West : 0.0
                            );

                            Base_Cell.Boundary = Bound;
                        }
                    }

                    Base_Cell.Read(BaseCell_FileInfo.OpenRead());

                    int Reference = ((2 * Base_Cell.Update_Number) + Cell_Collection.Count - 1) * Cell_Collection.Count / 2;
                    int Sum = Base_Cell.Update_Number + Cell_Collection.Select(Cell => Cell.UPDN).Sum();

                    if ((Base_Cell.DSID.DSNM == BaseCell_FileInfo.Name) && (Reference == Sum))
                    {
                        DetectionChart Detection_Chart = new DetectionChart(Base_Cell);
                        Detection_Chart.Name = chart_name;

                        foreach ((int UPDN, FileInfo File) Cell in Cell_Collection.Where(Cell => Cell.UPDN > 0).OrderBy(Cell => Cell.UPDN))
                        {
                            DetectionCell Update_Cell = new DetectionCell();
                            Update_Cell.Boundary = Detection_Chart.Boundary;
                            Update_Cell.Read(Cell.File.OpenRead());

                            if ((Update_Cell.DSID.DSNM == Cell.File.Name) && (Update_Cell.Update_Number == Cell.UPDN) && (Update_Cell.Edition_Number == Detection_Chart.Base.EDTN) && (Update_Cell.Update_Number > Detection_Chart.Update))
                            {
                                Detection_Chart.Absorb(Update_Cell);
                            }
                            else
                            {
                                return null;
                            }
                        }

                        return Detection_Chart;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
}