using Intel.RealSense;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;

namespace _3DScanning.Model
{
    class ScanManager
    {
        public IList<Camera> Cameras { get; set; }
        public int FramesNumber { get; set; }
        public int DummyFramesNumber { get; set; }
        public string Filename { get; set; }
        public bool Debug { get; set; }

        public ScanManager()
        {
            Cameras = new List<Camera>();
            FramesNumber = 15;
            DummyFramesNumber = 30;
            Filename = "default.stl";
            Debug = false;
        }

        public ScanManager(Context ctx) : this()
        {
            foreach (var device in ctx.QueryDevices())
            {
                Cameras.Add(new Camera(device.Info.GetInfo(CameraInfo.SerialNumber)));
            }
        }

        public List<Vector3> ScanObject()
        {
            var depthCams = Cameras.Where(c => c.Type == CameraType.Depth && c.On).ToList();
            var lidarCams = Cameras.Where(c => c.Type == CameraType.LiDAR && c.On).ToList();

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

        public List<Vector3> ScanObjectV2()
        {
            var depthCams = Cameras.Where(c => c.Type == CameraType.Depth && c.On).ToList();
            var lidarCams = Cameras.Where(c => c.Type == CameraType.LiDAR && c.On).ToList();

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


        public void Calibrate()
        {
            var depthCams = Cameras.Where(c => c.Type == CameraType.Depth && c.On).ToList();
            var lidarCams = Cameras.Where(c => c.Type == CameraType.LiDAR && c.On).ToList();

            var tasks = new List<Task>(depthCams.Count() + lidarCams.Count());

            foreach (var dcam in depthCams)
            {
                tasks.Add(Task.Run(() =>
                {
                    var frame = dcam.CaptureFrame(FramesNumber, DummyFramesNumber);
                    frame = dcam.ApplyFilters(frame);
                    var pointcloud = Utils.FrameToPointCloud(frame);
                    frame.Dispose();
                    var diff = Utils.AveragePointCloud(pointcloud);
                    dcam.Delta = new Vector3(-diff.X, -diff.Y, 0);
                    dcam.Distance = diff.Z;
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
                    var diff = Utils.AveragePointCloud(pointcloud);
                    lcam.Delta = new Vector3(-diff.X, -diff.Y, 0);
                    lcam.Distance = diff.Z;
                }));
            }
        }
    }
}