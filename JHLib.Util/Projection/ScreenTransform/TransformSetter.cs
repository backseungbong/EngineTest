using JHLib.Util.Graphic;
using JHLib.Util.Helper;
using JHLib.Util.Matrix;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Projection.ScreenTransform
{
    public delegate void UpdatedBackBuffer(DoubleBufferedBitmap dbBitmap);
    public delegate void UpdatedTransform(Transform updateTransform);
    public abstract class TransformSetter
    {
        // [bsb]
        private IMapProjection _projection = new MercatorProjection(); // 기본값을 Mercator로 구현하여 할당

        // 투영법을 런타임에 전환하는 메서드
        public Transform SetProjection(IMapProjection newProjection)
        {
            Transform updateTransform;
            lock (_locker)
            {
                // 1. 현재 화면이 바라보고 있는 경위도(WGS84) 중심 좌표를 가져옵니다.
                // (_transform이 아직 생성되기 전이라면 기본값 0, 0 처리)
                double currentLon = _transform != null ? _transform.WGS84Position.X : 0;
                double currentLat = _transform != null ? _transform.WGS84Position.Y : 0;

                // 2. 투영법 객체를 교체합니다.
                _projection = newProjection;

                // 3. [핵심] 새로운 투영법을 기준으로 월드 중심 좌표(transX, transY)를 다시 계산합니다!
                var newWorldPos = _projection.ToWorldD(currentLon, currentLat);
                var clampedPos = _projection.CheckProjectionRange(newWorldPos.X, newWorldPos.Y);
                _transX = clampedPos.X;
                _transY = clampedPos.Y;

                // 4. 새로운 상태로 Transform을 업데이트합니다.
                updateTransform = UpdateTransform();
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        private readonly Lock _locker;

        private int _width;
        private int _height;
        private double _pivotX;
        private double _pivotY;
        private double _pixelMM;
        private ScreenInfo _screenInfo;

        private uint _version;
        private double _transX;
        private double _transY;
        private double _rotation;
        private double _scale;

        private int _scalei;
        private bool _isCustomScale;

        protected volatile Transform _transform;

        /// <summary> 현재 Transform 반환 </summary>
        public Transform Transform => _transform;

        public event UpdatedTransform OnTransformChanging;
        protected abstract void ChangedTransform(Transform transform);
        public TransformSetter()
        {
            _locker = new();

            _width = 1;
            _height = 1;
            _pivotX = 0.5d;
            _pivotY = 0.5d;
            _pixelMM = 25.4d / 144d;

            _version = 0;
            _transX = 0;
            _transY = 0;
            _rotation = 0;
            _scale = TransformScale.SCALE_DEFAULT;

            _scalei = TransformScale.DEFAULT_SCALE_INDEX;
            _isCustomScale = false;

            UpdateScreen();
            UpdateTransform();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateScreen() => _screenInfo = new(_width, _height, _pivotX, _pivotY, _pixelMM);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Transform UpdateTransform(bool immediateTransformApply = true) =>
            _transform = new(_screenInfo, ++_version, _transX, _transY, _rotation, _scale, immediateTransformApply, _projection);

        /// <summary> Transform Version을 갱신하고, OnUpdateTransform 이벤트를 발생시킨다 </summary>      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Transform Invalidate()
        {
            Transform updateTransform;
            lock (_locker)
            {
                updateTransform = UpdateTransform();
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면 가로, 세로 해상도 및 대각선 실 크기(inch)를 설정한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform SetMoniter(int wResolution, int hResolution, double diagonalInch)
        {
            Transform updateTransform;
            lock (_locker)
            {
                _pixelMM = CheckPixelMM(diagonalInch * 25.4d /
                    Math.Sqrt(wResolution * wResolution + hResolution * hResolution));
                UpdateScreen();
                updateTransform = UpdateTransform();
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면 가로, 세로 해상도 및 실 크기(mm)를 설정한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform SetMoniter(int wResolution, int hResolution, double wMillimeter, double hMillimeter)
        {
            Transform updateTransform;
            lock (_locker)
            {
                _pixelMM = CheckPixelMM(Math.Sqrt(wMillimeter * wMillimeter + hMillimeter * hMillimeter) /
                    Math.Sqrt(wResolution * wResolution + hResolution * hResolution));
                UpdateScreen();
                updateTransform = UpdateTransform();
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면 dpi를 설정한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform SetDPI(double dpi)
        {
            Transform updateTransform;
            lock (_locker)
            {
                _pixelMM = CheckPixelMM(25.4d / dpi);
                UpdateScreen();
                updateTransform = UpdateTransform();
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면의 가로, 세로 영역을 설정한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform SetScreen(int width, int height)
        {
            if (width <= 0 || height <= 0)
                return _transform;

            Transform updateTransform;
            lock (_locker)
            {
                updateTransform = SetScreenCore(width, height);
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면의 가로, 세로 영역을 설정한다 (단순 설정용 및 동기 처리가 보장되는 상황, 커널용도로 사용 권장) </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform SetScreenCore(int width, int height)
        {
            _width = width;
            _height = height;
            UpdateScreen();
            return UpdateTransform();
        }

        /// <summary>
        /// 화면의 피봇을 상대 값으로 설정한다<para/>
        /// 피봇은 화면의 이동 및 회전시 중심점의 역할을한다 (기본값 : 0.5, 범위 : 0.0 ~ 1.0)
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform SetPivot(double pivotX, double pivotY)
        {
            Transform updateTransform;
            lock (_locker)
            {
                _pivotX = CheckPivot(pivotX);
                _pivotY = CheckPivot(pivotY);
                UpdateScreen();
                updateTransform = UpdateTransform();
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 
        /// 화면의 특정 위치를 피봇위치로 설정한다 <para/>
        /// 피봇은 화면의 이동 및 회전시 중심점의 역할을한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Transform SetPivotScreen(Float2D sp) => SetPivotScreen(sp.X, sp.Y);

        /// <summary> 
        /// 화면의 특정 위치를 피봇위치로 설정한다 <para/>
        /// 피봇은 화면의 이동 및 회전시 중심점의 역할을한다
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform SetPivotScreen(double sx, double sy)
        {
            Transform updateTransform;
            lock (_locker)
            {
                _pivotX = CheckPivot(sx / _width);
                _pivotY = CheckPivot(sy / _height);
                UpdateScreen();
                updateTransform = UpdateTransform();
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면 Rotation을 설정한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform SetRotation(double rotation, bool addRotation = false)
        {
            Transform updateTransform;
            lock (_locker)
            {
                _rotation = CheckRotation(addRotation ? _rotation + rotation : rotation);
                updateTransform = UpdateTransform();
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면을 줌인 한다 (대축척 방향) </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform ZoomIn()
        {
            Transform updateTransform;
            lock (_locker)
            {
                var index = ZoomInNextIndex();
                _scale = TransformScale.Scales[index];
                _scalei = index;
                _isCustomScale = false;

                updateTransform = UpdateTransform();
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면을 줌아웃 한다 (소축척 방향) </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform ZoomOut()
        {
            Transform updateTransform;
            lock (_locker)
            {
                var index = ZoomOutNextIndex();
                _scale = TransformScale.Scales[index];
                _scalei = index;
                _isCustomScale = false;

                updateTransform = UpdateTransform();
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면을 줌인 한다 (대축척 방향) </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform ZoomIn(double screenOriginX, double screenOriginY)
        {
            Transform updateTransform;
            lock (_locker)
            {
                var index = ZoomInNextIndex();
                _scale = TransformScale.Scales[index];
                _scalei = index;
                _isCustomScale = false;

                var wp = CalculateScaleAroundOriginPosition(_transform, _scale, screenOriginX, screenOriginY);
                SetTranslation(wp);
                updateTransform = UpdateTransform();
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면을 줌아웃 한다 (소축척 방향) </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform ZoomOut(double screenOriginX, double screenOriginY)
        {
            Transform updateTransform;
            lock (_locker)
            {
                var index = ZoomOutNextIndex();
                _scale = TransformScale.Scales[index];
                _scalei = index;
                _isCustomScale = false;

                var wp = CalculateScaleAroundOriginPosition(_transform, _scale, screenOriginX, screenOriginY);
                SetTranslation(wp);
                updateTransform = UpdateTransform();
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면 Scale을 설정한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform SetScale(double scale)
        {
            Transform updateTransform;
            lock (_locker)
            {
                updateTransform = SetScaleCore(scale);
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면 Scale을 설정한다 (단순 설정용 및 동기 처리가 보장되는 상황, 커널용도로 사용 권장) </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform SetScaleCore(double scale)
        {
            _scale = CheckScale(scale);
            _isCustomScale = true;
            return UpdateTransform();
        }

        /// <summary> 화면 Scale을 설정한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform SetScale(double scale, double screenOriginX, double screenOriginY)
        {
            Transform updateTransform;
            lock (_locker)
            {
                _scale = CheckScale(scale);
                _isCustomScale = true;

                var wp = CalculateScaleAroundOriginPosition(_transform, _scale, screenOriginX, screenOriginY);
                SetTranslation(wp);
                updateTransform = UpdateTransform();
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면 Scale을 설정한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform SetScaleAmount(double scaleAmount, double screenOriginX, double screenOriginY)
        {
            Transform updateTransform;
            lock (_locker)
            {
                _scale = CheckScale(_scale / scaleAmount);
                _isCustomScale = true;

                var wp = CalculateScaleAroundOriginPosition(_transform, _scale, screenOriginX, screenOriginY);
                SetTranslation(wp);
                updateTransform = UpdateTransform();
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면이동 및 ScaleAmount를 설정한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform MoveOrSetScale(
            double screenMoveX, double screenMoveY, double scaleAmount, double screenOriginX, double screenOriginY)
        {
            Transform updateTransform;

            lock (_locker)
            {
                var tf = _transform;
                var wp = tf.ScreenToWorldD(tf.PivotPositionX + screenMoveX, tf.PivotPositionY + screenMoveY);
                // [bsb 막음]
                //wp = EPSG3857.CheckProjectionRange(wp);
                var cp = _projection.CheckProjectionRange(wp.X, wp.Y);
                wp = new Double2D(cp.X, cp.Y);

                var pastScale = _scale;
                var lastScale = CheckScale(pastScale / scaleAmount);
                if (lastScale != pastScale)
                {
                    var px = tf.PivotPositionX;
                    var py = tf.PivotPositionY;
                    var tw = Matrix22D.Create(wp.X, -wp.Y, tf.Rotation, tf.FactorWorldToScreen, px, py).Invert();
                    wp = tw.Transform64PostFlipY(screenOriginX, screenOriginY);

                    _scale = lastScale;
                    _isCustomScale = true;

                    tw = Matrix22D.Create(wp.X, -wp.Y, tf.Rotation, tf.FactorMeToPixel / lastScale, px, py).Invert();
                    wp = tw.Transform64PostFlipY(px - (screenOriginX - px), py - (screenOriginY - py));
                }
                // [bsb 막음]
                //SetTranslation(EPSG3857.CheckProjectionRange(wp));
                SetTranslation(wp.X, wp.Y);
                updateTransform = UpdateTransform();
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면을 지정한 화면좌표로 이동시킨다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Transform MoveToScreen(Float2D sp, bool isRelative = false) => MoveToScreen(sp.X, sp.Y, isRelative);

        /// <summary> 화면을 지정한 화면좌표로 이동시킨다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform MoveToScreen(double sx, double sy, bool isRelative = false, bool immediateTransformApply = true)
        {
            Transform updateTransform;
            lock (_locker)
            {
                var tf = _transform;
                var rx = isRelative ? tf.PivotPositionX + sx : sx;
                var ry = isRelative ? tf.PivotPositionY + sy : sy;
                SetTranslation(tf.ScreenToWorldD(rx, ry));
                updateTransform = UpdateTransform(immediateTransformApply);
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }



        /// <summary> 화면을 지정한 월드좌표로 이동시킨다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Transform MoveToWorld(Float2D wp, bool isRelative = false) => MoveToWorld(wp.X, wp.Y, isRelative);

        /// <summary> 화면을 지정한 월드좌표로 이동시킨다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform MoveToWorld(double wx, double wy, bool isRelative = false, bool immediateTransformApply = true)
        {
            Transform updateTransform;
            lock (_locker)
            {
                var tf = _transform;
                var rx = isRelative ? tf.WorldPosition.X + wx : wx;
                var ry = isRelative ? tf.WorldPosition.Y + wy : wy;
                SetTranslation(rx, ry);
                updateTransform = UpdateTransform(immediateTransformApply);
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }


        /// <summary> 화면을 지정한 WGS84좌표로 이동시킨다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Transform MoveToWGS84(Float2D lonlat, bool isRelative = false) => MoveToWGS84(lonlat.X, lonlat.Y, isRelative);

        /// <summary> 화면을 지정한 WGS84좌표로 이동시킨다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform MoveToWGS84(double lon, double lat, bool isRelative = false, bool immediateTransformApply = true)
        {
            // [bsb 막음]
            //Transform updateTransform;
            //lock (_locker)
            //{
            //    var tf = _transform;
            //    var rx = isRelative ? tf.WGS84Position.X + lon : lon;
            //    var ry = isRelative ? tf.WGS84Position.Y + lat : lat;
            //    SetTranslation(EPSG3857.ToWorldD(rx, ry));
            //    updateTransform = UpdateTransform(immediateTransformApply);
            //}
            //ChangedTransform(updateTransform);
            //return updateTransform;

            Transform updateTransform;
            lock (_locker)
            {
                var tf = _transform;
                var rx = isRelative ? tf.WGS84Position.X + lon : lon;
                var ry = isRelative ? tf.WGS84Position.Y + lat : lat;
                SetTranslation(_projection.ToWorldD(rx, ry));
                updateTransform = UpdateTransform(immediateTransformApply);
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면을 지정한 WGS84좌표로 이동시킨다 (단순 설정용 및 동기 처리가 보장되는 상황, 커널용도로 사용 권장) </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void MoveToWGS84Simple(double lon, double lat)
        {
            // [bsb 막음]
            //SetTranslation(EPSG3857.ToWorldD(lon, lat));
            SetTranslation(_projection.ToWorldD(lon, lat));
            UpdateTransform();
        }

        /// <summary> 화면을 지정한 월드좌표로 이동 및 Scale을 설정한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform MoveToWorldOrSetScale(double wx, double wy, double scale, bool immediateTransformApply = true)
        {
            Transform updateTransform;
            lock (_locker)
            {
                _scale = CheckScale(scale);
                _isCustomScale = true;
                SetTranslation(wx, wy);
                updateTransform = UpdateTransform(immediateTransformApply);
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면을 지정한 월드좌표로 이동 및 Rotation을 설정한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform MoveToWorldOrSetRotation(double wx, double wy, double rotation, bool immediateTransformApply = true)
        {
            Transform updateTransform;
            lock (_locker)
            {
                _rotation = CheckRotation(rotation);
                SetTranslation(wx, wy);
                updateTransform = UpdateTransform(immediateTransformApply);
            }
            ChangedTransform(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면을 지정한 화면좌표만큼 이동시킨다  <para/>
        /// OnTransformChanged 대신 OnTransformChanging 발생시켜 다시 그리는 함수를 타지 않는다 <para/>
        /// 화면 렌더링을 즉시 다시 하지 않고 화면만 변형시키는 경우에 적합함 <para/>
        /// 예를들어 Gesture 기능에서 화면만 변형하는 용도로 사용 가능
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform MoveToScreenChanging(double screenMoveX, double screenMoveY)
        {
            Transform updateTransform;
            lock (_locker)
            {
                var tf = _transform;
                var wp = tf.ScreenToWorldD(tf.PivotPositionX + screenMoveX, tf.PivotPositionY + screenMoveY);
                SetTranslation(wp);
                updateTransform = UpdateTransform();
            }
            OnTransformChanging?.Invoke(updateTransform);
            return updateTransform;
        }

        /// <summary> 화면이동 및 ScaleAmount를 설정한다 <para/>
        /// OnTransformChanged 대신 OnTransformChanging 발생시켜 다시 그리는 함수를 타지 않는다 <para/>
        /// 화면 렌더링을 즉시 다시 하지 않고 화면만 변형시키는 경우에 적합함 <para/>
        /// 예를들어 Gesture 기능에서 화면만 변형하는 용도로 사용 가능
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Transform MoveOrSetScaleChanging(
            double screenMoveX, double screenMoveY, double scaleAmount, double screenOriginX, double screenOriginY)
        {
            Transform updateTransform;
            lock (_locker)
            {
                var tf = _transform;
                var wp = tf.ScreenToWorldD(tf.PivotPositionX + screenMoveX, tf.PivotPositionY + screenMoveY);
                // [bsb 막음]
                //wp = EPSG3857.CheckProjectionRange(wp);
                var cp = _projection.CheckProjectionRange(wp.X, wp.Y);
                wp = new Double2D(cp.X, cp.Y);

                var pastScale = _scale;
                var lastScale = CheckScale(pastScale / scaleAmount);
                if (lastScale != pastScale)
                {
                    var px = tf.PivotPositionX;
                    var py = tf.PivotPositionY;
                    var tw = Matrix22D.Create(wp.X, -wp.Y, tf.Rotation, tf.FactorWorldToScreen, px, py).Invert();
                    wp = tw.Transform64PostFlipY(screenOriginX, screenOriginY);

                    _scale = lastScale;
                    _isCustomScale = true;

                    tw = Matrix22D.Create(wp.X, -wp.Y, tf.Rotation, tf.FactorMeToPixel / lastScale, px, py).Invert();
                    wp = tw.Transform64PostFlipY(px - (screenOriginX - px), py - (screenOriginY - py));
                }
                SetTranslation(wp);
                updateTransform = UpdateTransform();
            }
            OnTransformChanging?.Invoke(updateTransform);
            return updateTransform;
        }

        private void SetTranslation(in Double2D wp) => SetTranslation(wp.X, wp.Y);
        private void SetTranslation(double wx, double wy)
        {
            // [bsb 막음]
            //var rp = EPSG3857.CheckProjectionRange(wx, wy);
            var rp = _projection.CheckProjectionRange(wx, wy);
            _transX = rp.X;
            _transY = rp.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ZoomInNextIndex()
        {
            if (_isCustomScale)
            {
                var s = _scale;
                var t = TransformScale.Scales;
                var i = 0;
                do if (t[i] < s) return i;
                while (++i < t.Length);
                return i - 1;
            }
            return CheckScaleIndex(_scalei + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ZoomOutNextIndex()
        {
            if (_isCustomScale)
            {
                var s = _scale;
                var t = TransformScale.Scales;
                var i = t.Length - 1;
                do if (t[i] > s) return i;
                while (--i >= 0);
                return 0;
            }
            return CheckScaleIndex(_scalei - 1);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CheckPixelMM(double pixelMM)
        {
            const double DEFAULT_PIXELMM = 0.2646d; // 96dpi
            const double MIN_PIXELMM = 0.0254d;
            const double MAX_PIXELMM = 1.0000d;

            if (MIN_PIXELMM <= pixelMM && pixelMM <= MAX_PIXELMM)
                return pixelMM;
            return DEFAULT_PIXELMM;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CheckPivot(double p)
        {
            return MathHelper.Clamp(p, 0, 1, 0.5d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CheckRotation(double r)
        {
            return MathHelper.Cycle(r, 0, 360);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CheckScale(double s)
        {
            return TransformScale.ClampScale(s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CheckScaleIndex(int i)
        {
            return TransformScale.ClampScaleIndex(i);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Double2D CalculateScaleAroundOriginPosition(
            Transform pastTransform, double targetScale, double screenOriginX, double screenOriginY)
        {
            var wp = pastTransform.ScreenToWorldD(screenOriginX, screenOriginY);
            var px = pastTransform.PivotPositionX;
            var py = pastTransform.PivotPositionY;
            var ws = pastTransform.FactorMeToPixel / targetScale;
            var tw = Matrix22D.Create(wp.X, -wp.Y, pastTransform.Rotation, ws, px, py).Invert();
            return tw.Transform64PostFlipY(px - (screenOriginX - px), py - (screenOriginY - py));
        }
    }
}