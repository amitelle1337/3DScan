using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using System.Diagnostics;

namespace _3DScan.Model
{
    /// <summary>
    /// Class <c>ScanManager</c> manages scanning, calibrating and synchronizing between cameras.
    /// </summary>
    /// <see cref="Camera"/>
    public class ScanManager
    {
        /// <value>The cameras that the manger can operate on.</value>
        public List<Camera> Cameras { get; set; }

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
            UpdateCameras(ctx);
        }

        /// <summary>
        /// Updates the cameras list with the cameras found in <paramref name="ctx"/>.
        /// </summary>
        /// <param name="ctx">The context to query the cameras from.</param>
        public void UpdateCameras(Context ctx)
        {
            foreach (var device in ctx.QueryDevices())
            {
                var serial = device.Info.GetInfo(CameraInfo.SerialNumber);
                if (Cameras.Exists(c => c.Serial == serial)) continue;

                Cameras.Add(new Camera(serial));
            }
        }

        /// <summary>
        /// Scans and maps resulting frames with corresponding camera to a value via <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The mapped type.</typeparam>
        /// <param name="func">A mapping function from DepthFrames (and a corresponding camera), to a desirable value.</param>
        /// <returns>An array of the resulted values, from each camera.</returns>
        /// <remarks>The function should dispose the frames.</remarks>
        /// <see cref="ScanToFunctionAsync{T}(Func{Camera, DepthFrame[], T})"/>
        public T[] ScanToFunction<T>(Func<Camera, DepthFrame[], T> func)
        {
            var depthCams = Cameras.Where(c => (c.Type == CameraType.Sereo_Depth) && c.On).ToList();
            var lightCams = Cameras.Where(c => ((c.Type == CameraType.LiDAR) || (c.Type == CameraType.Coded_Light)) && c.On).ToList();

            var tasks = new Task<T>[depthCams.Count + lightCams.Count];
            var idx = 0;

            // Depth cameras can capture simultaneously without the present of other types of cameras.
            // Therefore we need to synchronize the completion of capturing with depth cameras before capturing with other types of cameras.
            // This way we get maximum performance, rather than capturing simultaneously and 'waisting' time.
            using (var cde = new CountdownEvent(depthCams.Count))
            {
                foreach (var dcam in depthCams)
                {
                    tasks[idx++] = Task.Run(() =>
                    {
                        var frames = dcam.CaptureDepthFrames(FramesNumber, DummyFramesNumber);
                        cde.Signal();
                        return func(dcam, frames);
                    });
                }

                cde.Wait();
            }

            //LiDAR and Coded-Light cameras cannot capture simultaneously, capture synchronously and launch a calculation task
            foreach (var lcam in lightCams)
            {
                var frames = lcam.CaptureDepthFrames(FramesNumber, DummyFramesNumber);
                tasks[idx++] = Task.Run(() => func(lcam, frames));
            }

            // .GetAwaiter().GetResult() rather than .Result, so if an exception is thrown it'll re-throw our exception.
            return Task.WhenAll(tasks).GetAwaiter().GetResult();
        }

        /// <summary>
        /// An async version to <c>ScanToFunction</c>.
        /// </summary>
        /// <typeparam name="T">The mapped type.</typeparam>
        /// <param name="func">A mapping function from DepthFrames (and a corresponding camera), to a desirable value.</param>
        /// <returns>An array of the resulted values, from each camera.</returns>
        /// <remarks>The function should dispose the frames.</remarks>
        /// <see cref="ScanToFunction{T}(Func{Camera, DepthFrame[], T})"/>
        public async Task<T[]> ScanToFunctionAsync<T>(Func<Camera, DepthFrame[], T> func)
        {
            var depthCams = Cameras.Where(c => (c.Type == CameraType.Sereo_Depth) && c.On).ToList();
            var lightCams = Cameras.Where(c => ((c.Type == CameraType.LiDAR) || (c.Type == CameraType.Coded_Light)) && c.On).ToList();

            var tasks = new Task<T>[depthCams.Count + lightCams.Count];
            var idx = 0;

            // Depth cameras can capture simultaneously without the present of other types of cameras.
            // Therefore we need to synchronize the completion of capturing with depth cameras before capturing with other types of cameras.
            // This way we get maximum performance, rather than capturing simultaneously and 'waisting' time.
            var acde = new AsyncCountdownEvent(depthCams.Count);

            foreach (var dcam in depthCams)
            {
                tasks[idx++] = Task.Run(() =>
                {
                    var frames = dcam.CaptureDepthFrames(FramesNumber, DummyFramesNumber);
                    acde.Signal();
                    return func(dcam, frames);
                });
            }

            // ConfigureAwait(false) because the context does not matter.
            await acde.WaitAsync().ConfigureAwait(false);

            //LiDAR and Coded-Light cameras cannot capture simultaneously, capture synchronously and launch a calculation task
            foreach (var lcam in lightCams)
            {
                var frames = lcam.CaptureDepthFrames(FramesNumber, DummyFramesNumber);
                tasks[idx++] = Task.Run(() => func(lcam, frames));
            }

            // ConfigureAwait(false) because the context does not matter.
            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }


        /// <summary>
        /// Scans an object with the available cameras , and applies the appropriate transoms to each camera's output to get the desired result.
        /// </summary>
        /// <returns>The resulting point-cloud.</returns>
        public List<Vector3> ScanObject()
        {
            var pointcloud = new List<Vector3>();

            // ConfigureAwait(false) because the context does not matter.
            var pcs = ScanToFunction(ProcessPCFunc()).ToList();

            foreach (var pc in pcs)
            {
                pointcloud.AddRange(pc);
            }

            return pointcloud;
        }

        /// <summary>
        /// An async version of <see cref="ScanObject"/>.
        /// </summary>
        /// <returns>The resulting point-cloud.</returns>
        public async Task<List<Vector3>> ScanObjectAsync()
        {
            var pointcloud = new List<Vector3>();

            // ConfigureAwait(false) because the context does not matter.
            var pcs = (await ScanToFunctionAsync(ProcessPCFunc()).ConfigureAwait(false)).ToList();

            foreach (var pc in pcs)
            {
                pointcloud.AddRange(pc);
            }

            return pointcloud;
        }

        /// <summary>
        /// Returns the processing function to use after scanning.
        /// </summary>
        /// <returns>the processing function to use after scanning.</returns>
        /// <see cref="ScanObject"/>
        /// <see cref="ScanObjectAsync"/>
        private Func<Camera, DepthFrame[], List<Vector3>> ProcessPCFunc()
        {
            var onCams = Cameras.Where(c => c.On).ToList();
            onCams.Sort((c1, c2) => c1.Angle.CompareTo(c2.Angle));

            return (cam, frames) =>
            {
                var filteredFrame = cam.ApplyFilters(frames);
                frames.ToList().ForEach(f => f.Dispose());
                var pc = Utils.FrameToPointCloud(filteredFrame);
                filteredFrame.Dispose();

                cam.AdjustInPlace(pc);

                var i = onCams.FindIndex(c => c.Angle == cam.Angle);
                var before = i != 0 ? onCams[i - 1] : onCams[onCams.Count - 1];
                var after = i != onCams.Count - 1 ? onCams[i + 1] : onCams[0];

                var lowerBound = cam.FindCriticalAngle(before);
                var upperBound = cam.FindCriticalAngle(after);

                lowerBound = lowerBound < 0 ? lowerBound : -MathF.PI / 2;
                upperBound = upperBound > 0 ? upperBound : MathF.PI / 2;

                pc = pc.Where(v => lowerBound <= MathF.Tan(v.Z / v.X) && MathF.Tan(v.Z / v.X) <= upperBound).ToList();

                cam.RotateInPlace(pc);

                return pc;
            };
        }

        /// <summary>
        /// Scan the calibration object (assuming it's the object that is being captured), and calculates the deviations of the each camera from
        /// that object in (x, y, z). Updates each camera accordingly.
        /// </summary>
        /// <see cref="Camera.PositionDeviation"/>
        public void Calibrate()
        {
            ScanToFunction((cam, frames) =>
            {
                var filteredFrame = cam.ApplyFilters(frames);
                frames.ToList().ForEach(f => f.Dispose());
                var pointcloud = Utils.FrameToPointCloud(filteredFrame);
                filteredFrame.Dispose();
                var dev = Utils.Average(pointcloud);
                cam.PositionDeviation = new Vector3(-dev.X, -dev.Y, CalibraitionSurface.Z + dev.Z);
                return 0; // Dummy return value
            });
        }

        /// <summary>
        /// An async version of <see cref="Calibrate"/>.
        /// </summary>
        /// <see cref="Calibrate">
        public async Task CalibrateAsync()
        {
            await ScanToFunctionAsync((cam, frames) =>
            {
                var filteredFrame = cam.ApplyFilters(frames);
                frames.ToList().ForEach(f => f.Dispose());
                var pointcloud = Utils.FrameToPointCloud(filteredFrame);
                filteredFrame.Dispose();
                var dev = Utils.Average(pointcloud);
                cam.PositionDeviation = new Vector3(-dev.X, -dev.Y, CalibraitionSurface.Z + dev.Z);
                return 0; // Dummy return value
            });
        }

        /// <summary>
        /// Saves the point-cloud <paramref name="vertices"/> to a file in the format of <paramref name="fileExtension"/>.
        /// The file name is the <c>Filename</c> property in <c>ScanManager</c>.
        /// </summary>
        /// <param name="vertices">The point-cloud to save.</param>
        /// <param name="fileExtension">The extension of the file (The file's format).</param>
        public void SavePointCloud(IEnumerable<Vector3> vertices, string fileExtension = "xyz")
        {
            SavePointCloudAsync(vertices, fileExtension).Wait();
        }

        /// <summary>
        /// An async version of <see cref="SavePointCloud"/>.
        /// </summary>
        /// <param name="vertices">The point-cloud to save.</param>
        /// <param name="fileExtension">The extension of the file (The file's format).</param>
        /// <see cref="SavePointCloud(IEnumerable{Vector3}, string)"/>
        public async Task SavePointCloudAsync(IEnumerable<Vector3> vertices, string fileExtension = "xyz")
        {
            switch (fileExtension)
            {
                case "xyz":
                    await Utils.WriteXYZAsync($"{Filename}.{fileExtension}", vertices);
                    break;
                default:
                    throw new NotSupportedException($"The format {fileExtension} is not supported.");
            }
        }
    }
}