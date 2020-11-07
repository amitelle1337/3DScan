using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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

            var idx = 0;

            var depthCapture = new Task<DepthFrame[]>[depthCams.Count];
            foreach (var dcam in depthCams)
            {
                depthCapture[idx++] = dcam.CaptureDepthFramesAsync(FramesNumber, DummyFramesNumber);
            }

            var framesArrays = new DepthFrame[depthCams.Count + lightCams.Count][];

            // ConfigureAwait(false) because the context does not matter.
            var depthFramesArrays = await Task.WhenAll(depthCapture).ConfigureAwait(false);

            for (var i = 0; i < depthFramesArrays.Length; ++i)
            {
                framesArrays[i] = depthFramesArrays[i];
            }

            // LiDAR and Coded-Light cameras cannot capture simultaneously, capture synchronously and launch a calculation task.
            for (var i = depthCams.Count; i < framesArrays.Length; ++i)
            {
                // ConfigureAwait(false) because the context does not matter.
                framesArrays[i] = await lightCams[depthCams.Count - i].CaptureDepthFramesAsync(FramesNumber, DummyFramesNumber).ConfigureAwait(false);
            }

            var onCams = new List<Camera>(depthCams);
            onCams.AddRange(lightCams);

            var calcTasks = new Task<T>[framesArrays.Length];

            for (var i = 0; i < calcTasks.Length; ++i)
            {
                var finalI = i; // Captured parameter must be effectively final.
                calcTasks[finalI] = Task.Run(() => func(onCams[finalI], framesArrays[finalI]));
            }

            // ConfigureAwait(false) because the context does not matter.
            return await Task.WhenAll(calcTasks).ConfigureAwait(false);
        }

        /// <summary>
        /// An async version of <see cref="ScanObject"/>.
        /// </summary>
        /// <returns>The resulting point-cloud.</returns>
        public async Task<List<Vector3>> ScanObjectAsync()
        {

            var onCams = Cameras.Where(c => c.On).ToList();
            onCams.Sort((c1, c2) => c1.Angle.CompareTo(c2.Angle));

            // ConfigureAwait(false) because the context does not matter.
            var pcs = (await ScanToFunctionAsync((cam, frames) =>
            {
                var filteredFrame = cam.ApplyFilters(frames);
                frames.ToList().ForEach(f => f.Dispose());
                var pc = Utils.FrameToPointCloud(filteredFrame);
                filteredFrame.Dispose();

                cam.AdjustInPlace(pc);

                var i = onCams.FindIndex(c => c.Angle == cam.Angle);
                var before = 0 < i ? onCams[i - 1] : onCams[onCams.Count - 1];
                var after = i < onCams.Count - 1 ? onCams[i + 1] : onCams[0];

                var lowerBound = cam.FindCriticalAngle(before);
                var upperBound = cam.FindCriticalAngle(after);

                lowerBound = lowerBound < 0 ? lowerBound : -MathF.PI / 2;
                upperBound = upperBound > 0 ? upperBound : MathF.PI / 2;

                pc = pc.Where(v => lowerBound <= MathF.Tan(v.Z / v.X) && MathF.Tan(v.Z / v.X) <= upperBound).ToList();

                cam.RotateInPlace(pc);

                return pc;
            }).ConfigureAwait(false)).ToList();

            var pointcloud = new List<Vector3>();
            foreach (var pc in pcs)
            {
                pointcloud.AddRange(pc);
            }

            return pointcloud;
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
                return 0; // Dummy return value.
            });
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