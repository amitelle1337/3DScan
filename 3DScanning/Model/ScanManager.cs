﻿using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace _3DScanning.Model
{
    /// <summary>
    /// Class <c>ScanManager</c> manages scanning, calibrating and synchronizing between cameras.
    /// </summary>
    /// <see cref="Camera"/>
    public class ScanManager
    {
        /// <value>The cameras that the manger can operate on.</value>
        public IList<Camera> Cameras { get; set; }

        /// <value>The number of frames to capture from each camera when capturing.</value>
        public int FramesNumber { get; set; }

        /// <value>The number of frames to capture from each camera when capturing. Dummy frames are frames that are being captured but not saved in order to 'heat up' the camera.</value>
        public int DummyFramesNumber { get; set; }

        /// <value>The name of the file to save the result to.</value>
        public string Filename { get; set; }

        /// <value>The dimensions of the calibration surface (width, height, distance from center).</value>
        [JsonConverter(typeof(Vector3Converter))] public Vector3 CalibraitionSurface { get; set; }

        /// <value>Whether to add additional debug information.</value>
        public bool Debug { get; set; }


        /// <summary>
        /// Default constructor.
        /// </summary>
        public ScanManager()
        {
            Cameras = new List<Camera>();
            FramesNumber = 15;
            DummyFramesNumber = 30;
            Filename = "default";
            CalibraitionSurface = default;
            Debug = false;
        }

        /// <summary>
        /// Initializes a default <c>ScanManager</c> with the cameras found in <paramref name="ctx"/>.
        /// </summary>
        /// <param name="ctx">The context to query the cameras from.</param>
        public ScanManager(Context ctx) : this()
        {
            foreach (var device in ctx.QueryDevices())
            {
                Cameras.Add(new Camera(device.Info.GetInfo(CameraInfo.SerialNumber)));
            }
        }


        /// <summary>
        /// Scans an object with the available cameras , and applies the appropriate transoms to each camera's output to get the desired result.
        /// </summary>
        /// <returns>The resulting point-cloud.</returns>
        public List<Vector3> ScanObject()
        {
            var depthCams = Cameras.Where(c => (c.Type == CameraType.Depth) && c.On).ToList();
            var lidarCams = Cameras.Where(c => (c.Type == CameraType.LiDAR) && c.On).ToList();
            var onCams = new List<Camera>(depthCams);
            onCams.AddRange(lidarCams);
            onCams.Sort((c1, c2) => c1.Angle.CompareTo(c2.Angle));

            var tasks = new List<Task<List<Vector3>>>(depthCams.Count() + lidarCams.Count());

            foreach (var dcam in depthCams)
            {
                tasks.Add(Task.Run(() =>
                {
                    var frame = dcam.CaptureFrame(FramesNumber, DummyFramesNumber);
                    return FrameToPointCloud(dcam, frame);
                }));
            }

            //LiDAR cameras cannot capture simultaneously, capture synchronously and launch a calculation task
            foreach (var lcam in lidarCams)
            {
                var frame = lcam.CaptureFrame(FramesNumber, DummyFramesNumber);
                tasks.Add(Task.Run(() => FrameToPointCloud(lcam, frame)));
            }

            var pointcloud = new List<Vector3>();

            foreach (var t in tasks)
            {
                pointcloud.AddRange(t.Result);
            }

            return pointcloud;

            // Utility function
            List<Vector3> FrameToPointCloud(Camera cam, DepthFrame frame)
            {
                cam.ResetFilters();
                frame = cam.ApplyFilters(frame);
                var pointcloud = Utils.FrameToPointCloud(frame);
                frame.Dispose();
                cam.AdjustAndRotateInPlace(pointcloud);

                var idx = onCams.FindIndex(c => c.Angle == cam.Angle);
                var before = idx == 0 ? onCams.Last() : onCams[idx - 1];
                var after = idx == onCams.Count - 1 ? onCams.First() : onCams[idx + 1];

                var lowerBound = cam.FindCriticalAngle(before);
                var upperBound = cam.FindCriticalAngle(after);

                if (lowerBound == upperBound)
                {
                    if (lowerBound < 0)
                    {
                        upperBound = Math.PI;
                    }
                    else
                    {
                        lowerBound = -Math.PI;
                    }
                }

                //var filtered = pointcloud.Where(v => lowerBound <= Math.Tan(v.Z / v.X) && Math.Tan(v.Z / v.X) <= upperBound);

                return pointcloud;
            }
        }

        /// <summary>
        /// Scan the calibration object (assuming it's the object that is being captured), and calculates the deviations of the each camera from
        /// that object in (x, y, z). Updates each camera accordingly.
        /// </summary>
        /// <see cref="Camera.PositionDeviation"/>
        public void Calibrate()
        {
            var depthCams = Cameras.Where(c => (c.Type == CameraType.Depth) && c.On).ToList();
            var lidarCams = Cameras.Where(c => (c.Type == CameraType.LiDAR) && c.On).ToList();

            var tasks = new List<Task>(depthCams.Count() + lidarCams.Count());

            foreach (var dcam in depthCams)
            {
                tasks.Add(Task.Run(() =>
                {
                    var frames = dcam.CaptureFrames(FramesNumber, DummyFramesNumber);
                    FramesToAdjustDeviation(dcam, frames);
                }));
            }

            //LiDAR cameras cannot capture simultaneously, capture synchronously and launch a calculation task
            foreach (var lcam in lidarCams)
            {
                var frames = lcam.CaptureFrames(FramesNumber, DummyFramesNumber);
                tasks.Add(Task.Run(() => FramesToAdjustDeviation(lcam, frames)));
            }

            foreach (var task in tasks)
            {
                task.Wait();
            }

            // Utility function
            void FramesToAdjustDeviation(Camera cam, DepthFrame[] frames)
            {
                cam.ResetFilters();
                var frame = cam.ApplyFilters(frames);
                Utils.DisposeAll(frames);
                var pointcloud = Utils.FrameToPointCloud(frame);
                frame.Dispose();
                var dev = Utils.Average(pointcloud);
                cam.PositionDeviation = new Vector3(-dev.X, -dev.Y, CalibraitionSurface.Z + dev.Z);
            }
        }

        /// <summary>
        /// Saves the point-cloud <paramref name="vertices"/> to a file in the format of <paramref name="fileExtension"/>.
        /// The file name is the <c>Filename</c> property in <c>ScanManager</c>.
        /// </summary>
        /// <param name="vertices">The point-cloud to save.</param>
        /// <param name="fileExtension">The extension of the file (The file's format).</param>
        public void SavePointCloud(IEnumerable<Vector3> vertices, string fileExtension = "xyz")
        {
            switch (fileExtension)
            {
                case "xyz":
                    Utils.WriteXyz($"{Filename}.{fileExtension}", vertices);
                    break;
                default:
                    throw new NotSupportedException($"The format {fileExtension} is not supported.");
            }
        }
    }
}