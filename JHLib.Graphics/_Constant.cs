namespace JHLib.Graphics
{
    public enum DrawingStep : int { Step0, Step1, Step2, Step3, Step4, Step5, Step6, Step7, Step8, Step9 }
    public enum PositionType { Screen, World, WGS84 }
    public enum MovementType { Absolute, Relative }

    public delegate void PositionHandler(double x, double y, PositionType posType, MovementType movType = MovementType.Absolute);
}