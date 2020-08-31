using Intel.RealSense;
using System;

namespace _3DScan.Model
{
    /// <summary>
    /// Represents the technology the <c>Camera</c> uses.
    /// </summary>
    /// <see cref="Camera"/>
    public enum CameraType : ushort
    {
        /// <summary>
        /// The <c>Undefined</c> value represents a camera that the current version of the code does not support.
        /// </summary>
        Undefined,
        /// <summary>
        /// The <c>Sereo_Depth</c> value represents stereo-depth technology cameras. (e.g. D435i, D415, etc...)
        /// </summary>
        Sereo_Depth,
        /// <summary>
        /// The <c>LiDAR</c> value represents LiDAR technology cameras. (e.g. L515.)
        /// </summary>
        LiDAR,
        /// <summary>
        /// The <c>Coded_Light</c> value represents coded-light cameras. (e.g. SR305)
        /// </summary>
        Coded_Light
    }

    /// <summary>
    /// Utility functions that go with <c>CameraType</c>.
    /// </summary>
    public static class CameraTypeFunctions
    {
        /// <summary>
        /// Finds the <c>CameraType</c> of a device with serial number <paramref name="serial"/>.
        /// </summary>
        /// <param name="serial">The serial number of the device.</param>
        /// <returns>The <c>CameraType</c> of the device. If no device found throws exception.</returns>
        public static CameraType QuarryCameraType(string serial)
        {
            using (var ctx = new Context())
            {
                var devices = ctx.QueryDevices();
                foreach (var device in devices)
                {
                    if (device.Info.GetInfo(CameraInfo.SerialNumber) != serial) continue;

                    return CameraNameToCameraType(device.Info[CameraInfo.Name]);
                }
            }

            throw new Exception($"The device with serial number {serial} is not connected");
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
    }
}
