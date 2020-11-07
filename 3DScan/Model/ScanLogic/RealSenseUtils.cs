using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace _3DScan.Model
{
    /// <summary>
    /// Utility functions that go with <c>CameraType</c>.
    /// </summary>
    public static class RealSenseUtils
    {
        private static readonly Dictionary<string, Device> _cache = new Dictionary<string, Device>();

        /// <summary>
        /// Finds the <c>CameraType</c> of a device with serial number <paramref name="serial"/>.
        /// </summary>
        /// <param name="serial">The serial number of the device.</param>
        /// <returns>The <c>CameraType</c> of the device. If no device found throws exception.</returns>
        public static CameraType QuarryCameraType(string serial)
        {
            return CameraNameToCameraType(QuarryDevice(serial).Info[CameraInfo.Name]);
        }

        public static Sensor QuerryDepthSensor(string serial)
        {
            return QuarryDevice(serial).Sensors.First(s => s.Is(Extension.DepthSensor));
        }

        public static Device QuarryDevice(string serial)
        {
            if (!_cache.ContainsKey(serial))
            {
                using (var ctx = new Context())
                {
                    var devices = ctx.QueryDevices();
                    foreach (var device in devices)
                    {
                        _cache.Add(device.Info[CameraInfo.SerialNumber], device);
                    }
                }

                if (!_cache.ContainsKey(serial))
                {
                    throw new Exception($"The device with serial number {serial} is not connected");
                }
            }

            return _cache[serial];
        }

        /// <summary>
        /// Converts an Intel camera's name to it's respective <c>CameraType</c>.
        /// </summary>
        /// <param name="name">The camera's name, preferably using the <c>GetInfo</c> method.</param>
        /// <returns>The respective <c>CameraType</c>.</returns>
        public static CameraType CameraNameToCameraType(string name)
        {
            switch (name)
            {
                case "Intel RealSense D415":
                case "Intel RealSense D435":
                case "Intel RealSense D435I":
                case "Intel RealSense D455":
                    return CameraType.Sereo_Depth;
                case "Intel RealSense L515":
                    return CameraType.LiDAR;
                case "Intel RealSense SR305":
                    return CameraType.Coded_Light;
                default:
                    return CameraType.Undefined;
            }
        }


        public static void L500DepthSensorPreset(this Sensor sensor, L500VisualPreset preset)
        {
            switch (preset)
            {
                case L500VisualPreset.NoAmbient:
                    sensor.Options[Option.LaserPower].Value = 70;
                    sensor.Options[Option.ConfidenceThreshold].Value = 1;
                    sensor.Options[Option.MinDistance].Value = 245;
                    sensor.Options[Option.PostProcessingSharpening].Value = 1;
                    sensor.Options[Option.PreProcessingSharpening].Value = 0;
                    sensor.Options[Option.NoiseFilterLevel].Value = 3;
                    break;
                case L500VisualPreset.LowAmbient:
                    sensor.Options[Option.LaserPower].Value = 100;
                    sensor.Options[Option.ConfidenceThreshold].Value = 1;
                    sensor.Options[Option.MinDistance].Value = 95;
                    sensor.Options[Option.PostProcessingSharpening].Value = 1;
                    sensor.Options[Option.PreProcessingSharpening].Value = 0;
                    sensor.Options[Option.NoiseFilterLevel].Value = 3;
                    break;
                case L500VisualPreset.MaxRange:
                    sensor.Options[Option.LaserPower].Value = 100;
                    sensor.Options[Option.ConfidenceThreshold].Value = 1;
                    sensor.Options[Option.MinDistance].Value = 245;
                    sensor.Options[Option.PostProcessingSharpening].Value = 1;
                    sensor.Options[Option.PreProcessingSharpening].Value = 0;
                    sensor.Options[Option.NoiseFilterLevel].Value = 3;
                    break;
                case L500VisualPreset.ShortRange:
                    sensor.Options[Option.LaserPower].Value = 95;
                    sensor.Options[Option.ConfidenceThreshold].Value = 1;
                    sensor.Options[Option.MinDistance].Value = 95;
                    sensor.Options[Option.PostProcessingSharpening].Value = 1;
                    sensor.Options[Option.PreProcessingSharpening].Value = 0;
                    sensor.Options[Option.NoiseFilterLevel].Value = 3;
                    break;
                default:
                    throw new Exception("Unsupported Preset");
            }
        }
    }
}
