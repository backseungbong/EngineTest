using JHLib.Util.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace JHLib.Util.Converter
{
    public class TotalUnits
    {
        public static Units Unit;
        public static byte PositionUnit => Unit.PositionUnit;
        public static byte DistanceUnit => Unit.DistanceUnit;
        public static byte DepthHeightUnit => Unit.DepthHeightUnit;
        public static byte SpeedUnit => Unit.SpeedUnit;
    }

    public class Units
    {
        public byte PositionUnit = 0;
        public byte DistanceUnit = 0;
        public byte DepthHeightUnit = 0;
        public byte SpeedUnit = 0;
    }

    public static class UnitsManager
    {
        private static string _exePath = "";
        private static string _unitsFilePath = "";

        public static void Init(string exePath)
        {
            _exePath = exePath;
            var dir = Path.Combine(_exePath, "SetupInfo");
            if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);
            _unitsFilePath = Path.Combine(dir, "Unit.ini");

            LoadUnits();
        }

        public static void LoadUnits()
        {
            TotalUnits.Unit = JsonConfig.Load<Units>(_unitsFilePath);
            PositionConverter.ChangePositionUnit((EnumPositionUnit)TotalUnits.PositionUnit);
            DistanceConverter.ChangeDistanceUnit((EnumDistanceUnit)TotalUnits.DistanceUnit);
            DepthHeightConverter.ChangeDepthHeightUnit((EnumDepthHeightUnit)TotalUnits.DepthHeightUnit);
            SpeedConverter.ChangeSpeedUnit((EnumSpeedUnit)TotalUnits.SpeedUnit);
        }

        public static void SaveUnits()
        {
            JsonConfig.Save(_unitsFilePath, TotalUnits.Unit);
        }
    }
}
