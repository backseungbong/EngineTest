using JHLib.Util.Projection;
using JHLib.MilitaryCoordinateConverter;

namespace JHApp.ECDIS.ViewModels
{
    public class MainVM : VMBase
    {
        public MainVM() 
        {
            // WGS84 -> UTM -> MGRS
            double lat = 35.1025;
            double lon = 129.0403;
            var mgrs = Wgs84ToMgrsConverter.ConvertWgs84ToMgrs(lat, lon);
            // MGRS -> UTM -> WGS84
            var (newLat, newLon) = MgrsToWgs84Converter.ConvertMgrsToWgs84(mgrs);
        }

        private string _projectionName = "Mercator Projection";
        public string ProjectionName { get => _projectionName; set => SetProperty(ref _projectionName, value); }

        private bool _mercatorOK = true;
        public bool MercatorOK
        {
            get => _mercatorOK;
            set
            {
                if(SetProperty(ref _mercatorOK, value) == true)
                {
                    if (value)
                    {
                        ProjectionName = "Mercator Projection";
                        EcdisApp.LayerManager.SetProjection(new MercatorProjection());
                    }
                    else
                    {
                        ProjectionName = "Polar Projection";
                        EcdisApp.LayerManager.SetProjection(new PolarNorthProjection());
                    }
                    EcdisApp.S57Chart.S57ChartRenderer.ResetProjection();
                }
            }
        }
    }
}
