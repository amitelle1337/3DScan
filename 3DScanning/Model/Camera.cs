using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace _3DScanning.Model
{
    /// <summary>
    /// Represents the technology the <c>Camera</c> uses.
    /// </summary>
    /// <see cref="Camera"/>
    /// 
    public enum CameraType : ushort
    {
        /// <summary>
        /// The <c>Undefined</c> value represents a camera that the current version of the code does not support.
        /// </summary>
        Undefined = 'U',
        /// <summary>
        /// The <c>Depth</c> value represents a stereo-depth technology. e.g D435i, D415, etc...
        /// </summary>
        Depth = 'D',
        /// <summary>
        /// The <c>LiDAR</c> value represents a LiDAR technology. e.g L515.
        /// </summary>
        LiDAR = 'L'
    }

    /// <summary>
    /// Class <c>Camera</c> models a camera in three-dimensional space.
    /// </summary>
    /// <see cref="CameraType"/>
    public class Camera
    {
        private string _serial;

        /// <value>
        /// The serial number of the <c>Camera</c>.
        /// </value>
        /// <remarks>The value of <c>Type</c> and <c>vFOV</c> is automatically updated when this value is changed.</remarks>
        /// <seealso cref="Type"/>
        /// <seealso cref="FOV"/>
        public string Serial
        {
            get => _serial;
            set
            {
                _serial = value;
                Type = QuarryCameraType(_serial);
                var intrinsics = GetIntrinsics();
                FOV = new Vector2(intrinsics.FOV[0], intrinsics.FOV[1]);
            }
        }

        /// <value>
        /// The angle that the <c>Camera</c> is rotated around the middle point of the object, measured counterclockwise.
        /// </value>
        public float Angle { get; set; }

        /// <value>
        /// The field of view of the camera, measured in degrees.
        /// </value>
        /// <remarks>This value automatically updated then the <c>Serial</c> is changed.</remarks>
        /// <seealso cref="Serial"/>
        [JsonIgnore] public Vector2 FOV { get; private set; }

        /// <value>The type of the <c>Camera</c>, according to it's technology.</value>
        /// <remarks>This value automatically updated then the <c>Serial</c> is changed.</remarks>
        /// <seealso cref="CameraType"/>
        /// <seealso cref="Serial"/>
        [JsonIgnore] public CameraType Type { get; private set; }

        /// <value>
        /// The deviation of the <c>Camera</c> in (x,y,z) from the middle point of the object.
        /// </value>
        [JsonConverter(typeof(Vector3Converter))] public Vector3 PositionDeviation { get; set; }

        /// <value>
        /// The filter associated with the <c>Camera</c>.
        /// </value>
        [JsonConverter(typeof(ListProcessingBlockConverter))] public List<ProcessingBlock> Filters { get; set; }

        /// <value>
        /// Whether the camera is on or off.
        /// </value>
        public bool On { get; set; }

        /// <summary>
        /// Default constructor. Initializes a new <c>Camera</c> with zero values.
        /// </summary>
        /// <remarks>
        /// Created only for json purposes, therefore it is a private constructor.
        /// </remarks>
        private Camera()
        {
        }

        /// <summary>
        /// This constructor initializes a new <c>Camera</c> with serial number, <paramref name="serial"/>.
        /// </summary>
        /// <param name="serial">The serial number of the <c>Camera</c>. Mandatory parameter.</param>
        /// <param name="angle">The angle of the <c>Camera</c>, measured counterclockwise. Optional parameter, default is 0.</param>
        /// <param name="posDev">The position deviation of the <c>Camera</c> from (0,0,0). Optional parameter, default is (0,0,0).</param>
        /// <param name="on">Whether the <c>Camera</c> is on/off. Optional parameter, default is true.</param>
        public Camera(string serial, float angle = default, Vector3 posDev = default, bool on = true)
        {
            Serial = serial;
            Type = QuarryCameraType(Serial);
            Angle = angle;
            PositionDeviation = posDev;
            Filters = new List<ProcessingBlock>();
            On = on;
        }

        /// <summary>
        /// Finds the <c>CameraType</c> of a device with serial number <paramref name="serial"/>.
        /// </summary>
        /// <param name="serial">The serial number of the device.</param>
        /// <returns>The <c>CameraType</c> of the device. If no device found throws exception.</returns>
        /// <see cref="CameraType"/>
        public static CameraType QuarryCameraType(string serial)
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
                        return CameraType.Undefined;
                    }

                    return v;
                }
            }

            throw new Exception($"The device with serial number {serial} is not connected");
        }


        /// <summary>
        /// Gets the appropriate config for this camera.
        /// </summary>
        /// <returns>Returns the depth stream config with this camera's serial number.</returns>
        public Config GetConfig()
        {
            var config = new Config();
            config.EnableStream(Stream.Depth);
            config.EnableDevice(Serial);

            return config;
        }

        /// <summary>
        /// Gets the intrinsics of this <c>Camera</c>.
        /// </summary>
        /// <returns>This camera's intrinsics.</returns>
        public Intrinsics GetIntrinsics()
        {
            var config = GetConfig();
            var pipe = new Pipeline();

            var pp = pipe.Start(config);
            try
            {
                return pp.GetStream(Stream.Depth).As<VideoStreamProfile>().GetIntrinsics();
            }
            finally
            {
                pipe.Stop();
            }
        }

        /// <summary>
        /// Captures a certain number of frames from this <c>Camera</c>.
        /// </summary>
        /// <param name="framesNumber">The number of frames to capture.</param>
        /// <param name="dummyFramesNumber">The number of dummy frames to capture. Dummy frames are frames that are being captured but not saved in order to 'heat up' the camera.</param>
        /// <returns>An array of frames, whom have been captured by this <c>Camera</c>. </returns>
        /// <seealso cref="CaptureFrame(int, int)"/>
        public DepthFrame[] CaptureFrames(int framesNumber = 1, int dummyFramesNumber = 0)
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

        /// </summary>
        /// <param name="framesNumber">The number of frames to capture.</param>
        /// <param name="dummyFramesNumber">The number of dummy frames to capture. Dummy frames are frames that are being captured but not saved in order to 'heat up' the camera.</param>
        /// <returns>A of frame, whom has been the result of applying a temporal filter on frames that where capture by this <c>Camera</c>. </returns>
        /// <seealso cref="CaptureFrames(int, int)"/>
        /// <remarks>
        /// Practically the same as the previous method, but uses temporal filter during capture to preserve memory and 'stop-the-world' pauses when disposing all frames at once.
        /// </remarks>
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

        /// <summary>
        /// Resets filters' cache.
        /// </summary>
        public void ResetFilters()
        {
            var serializerOptions = new JsonSerializerOptions();
            serializerOptions.Converters.Add(new ProcessingBlockConverter());

            Filters = JsonSerializer.Deserialize<List<ProcessingBlock>>(JsonSerializer.Serialize(Filters, serializerOptions), serializerOptions);
        }

        /// <summary>
        /// Applies this camera's filters on the resulted frame after 'averaging' them with temporal filter.
        /// </summary>
        /// <param name="frames">A collection of frames to 'average' and apply this camera's filters on.</param>
        /// <returns>The outcome of applying a temporal filter on frames and then applying this camera's filters on the resulted frame.</returns>
        /// <seealso cref="ApplyFilters(DepthFrame)"/>
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

            return ApplyFilters(res);
        }

        /// <summary>
        /// Applies this camera's filters on the frame.
        /// </summary>
        /// <param name="frame">A frame to apply this camera's filters on.</param>
        /// <returns>The outcome of applying this camera's filters on the frame.</returns>
        /// <seealso cref="ApplyFilters(IEnumerable{DepthFrame})"/>
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

        /// <summary>
        /// Adjust a point-cloud with this camera's deviations, then rotates the point-cloud by this camera's angle.
        /// </summary>
        /// <param name="vertices">A point-cloud to apply this transformation on.</param>
        public void AdjustAndRotateInPlace(List<Vector3> vertices)
        {
            Utils.ChangeCoordinatesInPlace(vertices,
                v => new Vector3(-(v.X + PositionDeviation.X), -(v.Y + PositionDeviation.Y), PositionDeviation.Z - v.Z));

            Utils.RotateAroundYAxisInPlace(vertices, Angle);
        }

        public double FindCriticalAngle(Camera other)
        {
            var halfPi = Math.PI / 2;
            var d1 = PositionDeviation.Z;
            var fov1 = Utils.ToRadians(FOV.X);
            var d2 = other.PositionDeviation.Z;
            var fov2 = Utils.ToRadians(other.FOV.X);
            var deltaAngle = Utils.ToRadians(Angle - other.Angle);

            var x = (d2 * Math.Sin(fov2) / Math.Sin(deltaAngle + fov2) - d1) / (Utils.Cot(fov1) - Math.Tan(halfPi + deltaAngle + fov2));
            var z = Utils.Cot(fov1) * x + d1;

            return Math.Atan(z / x);
        }
    }
}