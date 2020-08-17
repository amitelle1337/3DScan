using Intel.RealSense;
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
            Filename = "default.stl";
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
        /// <remarks>Uses the <c>CaptureFrames</c> instead of <c>CaptureFrame</c>, the 'default' implantation. Benchmark is needed.</remarks>
        /// <seealso cref="ScanObjectV2"/>
        /// <see cref="Camera.CaptureFrames(int, int)"/>
        /// <see cref="Camera.CaptureFrame(int, int)"/>
        public List<Vector3> ScanObject()
        {
            var depthCams = Cameras.Where(c => (c.Type == CameraType.Depth) && (c.On)).ToList();
            var lidarCams = Cameras.Where(c => (c.Type == CameraType.LiDAR) && (c.On)).ToList();

            var tasks = new List<Task<List<Vector3>>>(depthCams.Count() + lidarCams.Count());

            foreach (var dcam in depthCams)
            {
                tasks.Add(Task.Run(() =>
                {
                    var frames = dcam.CaptureFrames(FramesNumber, DummyFramesNumber);
                    var frame = dcam.ApplyFilters(frames);
                    Utils.DisposeAll(frames);
                    var pointcloud = Utils.FrameToPointCloud(frame);
                    frame.Dispose();
                    dcam.AdjustAndRotateInPlace(pointcloud);
                    return pointcloud;
                }));
            }

            //LiDAR cameras cannot capture simultaneously, capture synchronously and launch a calculation task
            foreach (var lcam in lidarCams)
            {
                var frames = lcam.CaptureFrames(FramesNumber, DummyFramesNumber);
                tasks.Add(Task.Run(() =>
                {
                    var frame = lcam.ApplyFilters(frames);
                    Utils.DisposeAll(frames);
                    var pointcloud = Utils.FrameToPointCloud(frame);
                    frame.Dispose();
                    lcam.AdjustAndRotateInPlace(pointcloud);
                    return pointcloud;
                }));
            }

            var pointcloud = new List<Vector3>();

            foreach (var t in tasks)
            {
                pointcloud.AddRange(t.Result);
            }

            return pointcloud;
        }

        /// <summary>
        /// Scans an object with the available cameras , and applies the appropriate transoms to each camera's output to get the desired result.
        /// </summary>
        /// <returns>The resulting point-cloud.</returns>
        /// <remarks>Uses the <c>CaptureFrame</c> instead of <c>CaptureFrames</c> in order to achieve better performance. Benchmark is needed.</remarks>
        /// <seealso cref="ScanObjectV2"/>
        /// <see cref="Camera.CaptureFrames(int, int)"/>
        /// <see cref="Camera.CaptureFrame(int, int)"/>
        public List<Vector3> ScanObjectV2()
        {
            var depthCams = Cameras.Where(c => (c.Type == CameraType.Depth) && (c.On)).ToList();
            var lidarCams = Cameras.Where(c => (c.Type == CameraType.LiDAR) && (c.On)).ToList();

            var tasks = new List<Task<List<Vector3>>>(depthCams.Count() + lidarCams.Count());

            foreach (var dcam in depthCams)
            {
                tasks.Add(Task.Run(() =>
                {
                    var frame = dcam.CaptureFrame(FramesNumber, DummyFramesNumber);
                    frame = dcam.ApplyFilters(frame);
                    var pointcloud = Utils.FrameToPointCloud(frame);
                    frame.Dispose();
                    dcam.AdjustAndRotateInPlace(pointcloud);
                    return pointcloud;
                }));
            }

            //LiDAR cameras cannot capture simultaneously, capture synchronously and launch a calculation task
            foreach (var lcam in lidarCams)
            {
                var frame = lcam.CaptureFrame(FramesNumber, DummyFramesNumber);
                tasks.Add(Task.Run(() =>
                {
                    frame = lcam.ApplyFilters(frame);
                    var pointcloud = Utils.FrameToPointCloud(frame);
                    frame.Dispose();
                    lcam.AdjustAndRotateInPlace(pointcloud);
                    return pointcloud;
                }));
            }

            var pointcloud = new List<Vector3>();

            foreach (var t in tasks)
            {
                pointcloud.AddRange(t.Result);
            }

            return pointcloud;
        }

        /// <summary>
        /// Scan the calibration object (assuming it's the object that is being captured), and calculates the deviations of the each camera from
        /// that object in (x, y, z). Updates each camera accordingly.
        /// </summary>
        /// <see cref="Camera.PositionDeviation"/>
        public void Calibrate()
        {
            var depthCams = Cameras.Where(c => (c.Type == CameraType.Depth) && (c.On)).ToList();
            var lidarCams = Cameras.Where(c => (c.Type == CameraType.LiDAR) && (c.On)).ToList();

            var tasks = new List<Task>(depthCams.Count() + lidarCams.Count());

            foreach (var dcam in depthCams)
            {
                tasks.Add(Task.Run(() =>
                {
                    var frame = dcam.CaptureFrame(FramesNumber, DummyFramesNumber);
                    frame = dcam.ApplyFilters(frame);
                    var pointcloud = Utils.FrameToPointCloud(frame);
                    frame.Dispose();
                    var dev = Utils.Average(pointcloud);
                    dcam.PositionDeviation = new Vector3(-dev.X, -dev.Y, dev.Z);
                }));
            }

            //LiDAR cameras cannot capture simultaneously, capture synchronously and launch a calculation task
            foreach (var lcam in lidarCams)
            {
                var frame = lcam.CaptureFrame(FramesNumber, DummyFramesNumber);
                tasks.Add(Task.Run(() =>
                {
                    frame = lcam.ApplyFilters(frame);
                    var pointcloud = Utils.FrameToPointCloud(frame);
                    frame.Dispose();
                    var dev = Utils.Average(pointcloud);
                    lcam.PositionDeviation = new Vector3(-dev.X, -dev.Y, dev.Z);
                }));
            }
        }
    }
}