using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;

namespace _3DScanning.Model
{
    public enum CameraType : ushort
    {
        Depth = 'D',
        LiDAR = 'L'
    }

    public class Camera
    {
        public string Serial { get; set; }
        public float Distance { get; set; }
        public float Angle { get; set; }
        public CameraType Type { get; }
        public float Dx { get; set; }
        public float Dy { get; set; }
        public float Dz { get; set; }
        [JsonIgnore] public IList<ProcessingBlock> Filters { get; set; }
        public bool On { get; set; }

        public Camera(string serial, float distance = 0, float angle = 0, float dx = 0, float dy = 0, float dz = 0,
            bool on = true)
        {
            Serial = serial;
            Type = GetCameraType(Serial);
            Distance = distance;
            Angle = angle;
            Dx = dx;
            Dy = dy;
            Dz = dz;
            Filters = new List<ProcessingBlock>();
            On = on;
        }

        public static CameraType GetCameraType(string serial)
        {
            using (var ctx = new Context())
            {
                var devices = ctx.QueryDevices();
                foreach (var device in devices)
                {
                    if (device.Info.GetInfo(CameraInfo.SerialNumber) != serial) continue;

                    var name = device.Info.GetInfo(CameraInfo.Name);
                    var lastSpaceIdx = name.LastIndexOf(' ');
                    var v = (CameraType)name[lastSpaceIdx + 1];

                    if (!Enum.IsDefined(typeof(CameraType), v))
                    {
                        throw new Exception(
                            $"Camera with type {name.Substring(lastSpaceIdx + 1)} is not supported");
                    }

                    return v;
                }
            }

            throw new Exception($"The device with serial number {serial} is not connected");
        }

        public Config GetConfig()
        {
            var config = new Config();
            config.EnableStream(Stream.Depth);
            config.EnableDevice(Serial);

            return config;
        }

        public DepthFrame[] CaptureFrames(int framesNumber = 1, int dummyFramesNumber = 0)
        {
            var config = GetConfig();
            var pipe = new Pipeline();

            var pp = pipe.Start(config);
            try
            {
                for (var i = 0; i < dummyFramesNumber; ++i)
                {
                    using (var frameset = pipe.WaitForFrames()) ;
                }

                var framesArr = new DepthFrame[framesNumber];

                for (var i = 0; i < framesNumber; ++i)
                {
                    using var frameset = pipe.WaitForFrames();
                    framesArr[i] = frameset.DepthFrame;
                }

                return framesArr;
            }
            finally
            {
                pipe.Stop();
            }
        }

        // Same as the previous method, but uses temporal filter during capture to preserve memory
        // and 'stop-the-world' pauses when disposing all frames at once.
        public DepthFrame CaptureFrame(int framesNumber = 1, int dummyFramesNumber = 0)
        {
            var config = GetConfig();
            var pipe = new Pipeline();

            var pp = pipe.Start(config);
            try
            {
                for (var i = 0; i < dummyFramesNumber; ++i)
                {
                    using (pipe.WaitForFrames()) ;
                }

                using var firstFrameset = pipe.WaitForFrames();
                var frame = firstFrameset.DepthFrame;
                var temporalFilter = new TemporalFilter();

                // Release temporary frames
                using var realeser = new FramesReleaser();

                for (var i = 0; i < framesNumber - 1; ++i)
                {
                    using var frameset = pipe.WaitForFrames();
                    using var depth = frameset.DepthFrame;
                    frame = temporalFilter.Process<DepthFrame>(depth);

                    if (i < framesNumber - 2)
                    {
                        frame.DisposeWith(realeser);
                    }
                }

                return frame;
            }
            finally
            {
                pipe.Stop();
            }
        }

        public DepthFrame ApplyFilters(IEnumerable<DepthFrame> frames)
        {
            var res = frames.First(_ => true);
            var tempFilter = new TemporalFilter();

            // Release temporary frames
            using var realeser = new FramesReleaser();

            var i = 0;
            foreach (var f in frames)
            {
                res = tempFilter.Process<DepthFrame>(f);

                // Release each frame which is not the last
                if (i < frames.Count() - 1)
                {
                    res.DisposeWith(realeser);
                }

                ++i;
            }

            i = 0;
            foreach (var filter in Filters)
            {
                res = filter.Process<DepthFrame>(res);

                // Release each frame which is not the last
                if (i < Filters.Count() - 1)
                {
                    res.DisposeWith(realeser);
                }

                ++i;
            }

            return res;
        }

        public DepthFrame ApplyFilters(DepthFrame frame)
        {
            var res = frame;

            // Release temporary frames
            using var realeser = new FramesReleaser();

            var i = 0;
            foreach (var filter in Filters)
            {
                res = filter.Process<DepthFrame>(res);

                // Release each frame which is not the last
                if (i < Filters.Count() - 1)
                {
                    res.DisposeWith(realeser);
                }

                ++i;
            }

            return res;
        }

        public void AdjustAndRotateInPlace(List<Vector3> vertices)
        {
            Utils.ChangeCoordinatesInPlace(vertices,
                v => new Vector3(-(v.X + Dx), -(v.Y + Dy), Distance - (v.Z + Dz)));

            Utils.RotatePointCloudInPlace(vertices, Angle);
        }
    }
}
