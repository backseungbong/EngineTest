using JHLib.Graphics;
using JHLib.S57.Chart;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using JHLib.Weather;
using SkiaSharp;

namespace JHLib.S57.ChartObject
{
    public class SOUNDG
    {
        public SOUNDG(S57ChartRenderer chartRenderer)
        {
			ChartRenderer = chartRenderer;
		}

        public const float ScaleFactor = 10000000.0f;

        // 현재 그려질 차트의 Usage를 가지고 있을 변수 
        public byte Usage = 255;

        // Main정보를 저장할 인스턴스 
        public S57ChartRenderer ChartRenderer = null;
        public ST_SOUNDG_HEADER Header;
        public ST_SOUND [] Sounds;

        public bool IsQuerySelect = false;
        private Float2D _querySelectPos = new Float2D(float.MaxValue, float.MaxValue);

        public Float2D[] SoundPoints = null;

        public void CreateSoundg(Transform projection)
        {
            int nSize = Sounds.Length;
            if (nSize > 0)
            {
                SoundPoints = new Float2D[nSize];
                for(int i=0; i<nSize; i++)
                {
                    float X = Sounds[i].XCOO / ScaleFactor;
                    float Y = Sounds[i].YCOO / ScaleFactor;
                    SoundPoints[i] = projection.WGS84ToScreen(X, Y);
                }
            }
        }

        // Draw함수 
        public void Draw(GraphicsContext context, ref ST_OVER over)
        {
            if (IsDraw(Header.UpdateType) == false) return;
            if (ChartRenderer.FindViewingGroup(13) == false || ChartRenderer.CheckScaleMin(Header.ScaleMin, context.Transform) == false) return;

            CreateSoundg(context.Transform);

            for(int k=0; k<Sounds.Length; k++)
            {
                if(context.Transform.PointContainScreen(SoundPoints[k]) == true)
                {
                    var sound = Sounds[k];

                    if ((char)sound.Sound1 != 'o')
                    {
                        string strSound = null;
                        if (S57ChartSafetyValue.SafetyDepth >= sound.Soundg) strSound = "SOUNDS";
                        else strSound = "SOUNDG";

                        string strAdd = null;
                        char[] chArr = new char[] { (char)sound.Sound1, (char)sound.Sound2, (char)sound.Sound3, (char)sound.Sound4 };
                        strAdd = new string(chArr);
                        int nLen = strAdd.Length / 2;
                        for (int m = 0; m < nLen; m++)
                        {
                            var strName = strSound + strAdd.Substring(m * 2, 2);
                            if (strName.Length == 8)
                            {
                                var nSymIndex = ChartRenderer.GetSymbolIndex(strName);
                                ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, SoundPoints[k], Header.UpdateType);
                            }
                        }
                    }

                    List<int> listSY = new List<int>();
                    ChartRenderer.CS_SNDFRM04(sound.Soundg, listSY);
                    foreach (var sy in listSY)
                    {
                        ChartRenderer.DrawSymbol(context, sy, 0.0f, SoundPoints[k], Header.UpdateType);
                    }
                }
            }

            // Query 선택 표시
            if (IsQuerySelect == true && _querySelectPos.X != float.MaxValue && _querySelectPos.Y != float.MaxValue) DrawSelectQueryPoint(context, _querySelectPos);
        }

        // Query함수
        public bool Query(Transform projection, Float2D point)
        {
            if (ChartRenderer.IsQuery == false || S57ChartQueryOptions.QueryPointOn == false || Header.UpdateType == 2 || Header.UpdateType == 12) return false;
            if (ChartRenderer.CheckScaleMin(Header.ScaleMin, projection) == false) return false;

            int offset = 10;
            int nSize = Sounds.Length;
            for (int i = 0; i < nSize; i++)
            {
                float X = Sounds[i].XCOO / ScaleFactor;
                float Y = Sounds[i].YCOO / ScaleFactor;
                Float2D ptPivot = projection.WGS84ToScreen((double)X, (double)Y);

                var rect = new SKRect((int)(ptPivot.X - offset), (int)(ptPivot.Y - offset), (int)(ptPivot.X + offset), (int)(ptPivot.Y + offset));

                IsQuerySelect = rect.Contains(point.X, point.Y);

                if (IsQuerySelect == true)
                {
                    _querySelectPos = new Float2D(X, Y);
                    break;
                }
            }

            return IsQuerySelect;
        }

        // Query정보를 Reset하는 함수 
        public void ResetQuery()
        {
            IsQuerySelect = false;
            _querySelectPos = new Float2D(float.MaxValue, float.MaxValue);
        }

        public void DrawSelectQueryPoint(GraphicsContext context, Float2D pivot)
        {
            if (IsQuerySelect == false) return;
            if (pivot.X == float.MaxValue || pivot.Y == float.MaxValue) return;

            pivot = context.Transform.WGS84ToScreen(pivot);

            var rgb = WeatherColor.GetColor("CURSR");
            context.SetStrokeColor(new SKColor(rgb.R, rgb.G, rgb.B));
            context.SetStrokeWidth(2);

            var offset = 20f;
            var space = 8f;
            context.DrawLine(new Float2D(pivot.X - offset, pivot.Y - offset), new Float2D(pivot.X - offset + space, pivot.Y - offset));
            context.DrawLine(new Float2D(pivot.X - offset, pivot.Y - offset), new Float2D(pivot.X - offset, pivot.Y - offset + space));

            context.DrawLine(new Float2D(pivot.X + offset, pivot.Y - offset), new Float2D(pivot.X + offset - space, pivot.Y - offset));
            context.DrawLine(new Float2D(pivot.X + offset, pivot.Y - offset), new Float2D(pivot.X + offset, pivot.Y - offset + space));

            context.DrawLine(new Float2D(pivot.X - offset, pivot.Y + offset), new Float2D(pivot.X - offset + space, pivot.Y + offset));
            context.DrawLine(new Float2D(pivot.X - offset, pivot.Y + offset), new Float2D(pivot.X - offset, pivot.Y + offset - space));

            context.DrawLine(new Float2D(pivot.X + offset, pivot.Y + offset), new Float2D(pivot.X + offset - space, pivot.Y + offset));
            context.DrawLine(new Float2D(pivot.X + offset, pivot.Y + offset), new Float2D(pivot.X + offset, pivot.Y + offset - space));
        }

        // Manual Update Query
        public bool MUquery(Transform projection, Float2D point, ref int index, ref Float2D pivot)
        {
            if (Header.UpdateType == 2 || Header.UpdateType == 12) return false;
            if (ChartRenderer.CheckScaleMin(Header.ScaleMin, projection) == false) return false;

            int offset = 10;
            int nSize = Sounds.Length;
            for (int i = 0; i < nSize; i++)
            {
                pivot.X = Sounds[i].XCOO / ScaleFactor;
                pivot.Y = Sounds[i].YCOO / ScaleFactor;
                var sxy = projection.WGS84ToScreen(pivot);

                var rect = new SKRect((int)(sxy.X - offset), (int)(sxy.Y - offset), (int)(sxy.X + offset), (int)(sxy.Y + offset));

                var select = rect.Contains(point.X, point.Y);
                if (select)
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        public void GetMUqueryResult(int index, List<MUsymbolInfo> listSY)
        {
            if (index >= Sounds.Length || index < 0) return;

            List<int> symbols = new();
            ChartRenderer.CS_SNDFRM04(Sounds[index].Soundg, symbols);
            foreach (var sy in symbols) listSY.Add(new MUsymbolInfo(sy));
        }

        public void Dispose()
        {
        }

        // Object를 그릴 조건이 되는지 확인하는 함수
        public bool IsDraw(byte updateType)
        {
            bool drawFlag = true;

            // Delete를 2->12로 만든 이유는 업데이트를 .001, .002 순차적으로 진행할 때 .001에서 삭제한 Object도 2 이고, .002에서 삭제한 Object도 2라서 
            // .001에서 삭제된 Object는 아예 Update Review를 하더라도 그리지 않게 해야 해서 마지막 Update의 경우에만 11,12,13처럼 10을 붙여서 보내주기로 함
            // Delete일때는 Review가 켜져있을 때만 그린다.
            if (updateType == 12) drawFlag = S57ChartOption.UpdateReview;
            else if (updateType == 2) drawFlag = false;

            return drawFlag;
        }
    }
}
