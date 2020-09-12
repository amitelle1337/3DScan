using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;

namespace _3DScan.Model
{
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
        /// <remarks>The value of <c>Type</c> and <c>vFOV</c> is automatically updated (lazily) when this value is changed.</remarks>
        /// <seealso cref="Type"/>
        /// <seealso cref="FOV"/>
        public string Serial
        {
            get => _serial;
            set
            {
                _serial = value;
                _type = null;
                _fov = null;
            }
        }

        /// <value>
        /// The angle that the <c>Camera</c> is rotated around the middle point of the object, measured counter-clockwise.
        /// </value>
        public float Angle { get; set; }

        private Vector2? _fov;

        /// <value>
        /// The field of view of the camera, measured in degrees.
        /// </value>
        /// <remarks>This value automatically updated (lazily) then the <c>Serial</c> is changed.</remarks>
        /// <seealso cref="Serial"/>
        [JsonIgnore]
        public Vector2 FOV
        {
            get
            {
                if (!_fov.HasValue)
                {
                    var intrinsics = GetDepthIntrinsics();
                    _fov = new Vector2(intrinsics.FOV[0], intrinsics.FOV[1]);
                }

                return _fov.Value;

            }
            private set => _fov = value;
        }

        private CameraType? _type;

        /// <value>The type of the <c>Camera</c>, according to it's technology.</value>
        /// <remarks>This value automatically updated (lazily) then the <c>Serial</c> is changed.</remarks>
        /// <seealso cref="CameraType"/>
        /// <seealso cref="Serial"/>
        [JsonIgnore]
        public CameraType Type
        {
            get
            {
                if (_type.HasValue)
                {
                    _type = CameraTypeFunctions.QuarryCameraType(Serial);
                }
                return _type.Value;
            }
            private set => _type = value;
        }

        /// <value>
        /// The deviation of the <c>Camera</c> in (x,y,z) from the middle point of the object.
        /// </value>
        [JsonConverter(typeof(Vector3Converter))] public Vector3 PositionDeviation { get; set; }

        public DecimationFilterWarpper DecimationWrapper { get; set; }
        public SpatialFilterWarpper SpatialWrapper { get; set; }
        public TemporalFilterWrapper TemporalWrapper { get; set; }
        public HoleFillingFilterWrapper HoleFillingWrapper { get; set; }
        public ThresholdFilterWrapper ThresholdWrapper { get; set; }

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
        /// <param name="angle">The angle of the <c>Camera</c>, measured counter-clockwise. Optional parameter, default is 0.</param>
        /// <param name="posDev">The position deviation of the <c>Camera</c> from (0,0,0). Optional parameter, default is (0,0,0).</param>
        /// <param name="on">Whether the <c>Camera</c> is on/off. Optional parameter, default is true.</param>
        public Camera(string serial, float angle = default, Vector3 posDev = default, bool on = true)
        {
            Serial = serial;
            Angle = angle;
            PositionDeviation = posDev;
            On = on;
            DecimationWrapper = new DecimationFilterWarpper();
            SpatialWrapper = new SpatialFilterWarpper();
            TemporalWrapper = new TemporalFilterWrapper();
            HoleFillingWrapper = new HoleFillingFilterWrapper();
            ThresholdWrapper = new ThresholdFilterWrapper();
        }

        /// <summary>
        /// The filters have a recommended order of activation.
        /// This method returns the custom wrappers in that order.
        /// </summary>
        /// <returns>The filter wrappers in the recommended order of activation.</returns>
        public FilterWrapper[] GetWrappersInOrder()
        {
            return new FilterWrapper[] { DecimationWrapper, SpatialWrapper, TemporalWrapper, HoleFillingWrapper, ThresholdWrapper };
        }

        /// <summary>
        /// The filters have a recommended order of activation.
        /// This method returns the filters in that order.
        /// </summary>
        /// <returns>The filter in the recommended order of activation.</returns>
        /// <remarks>The caller need to dispose the filters.</remarks>
        public List<ProcessingBlock> GetOnFiltersInOrder()
        {
            var blocks = new List<ProcessingBlock>();

            foreach (var wrapper in GetWrappersInOrder())
            {
                if (wrapper.On)
                {
                    blocks.Add(wrapper.GetFilter());
                }
            }

            return blocks;
        }

        /// <summary>
        /// Gets the appropriate config for this camera.
        /// </summary>
        /// <returns>Returns the depth stream config with this camera's serial number.</returns>
        public Config GetDepthConfig()
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
        public Intrinsics GetDepthIntrinsics()
        {
            var config = GetDepthConfig();
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
        /// <param name="framesNumber">The number of frames to capture. Default is <c>1</c>.</param>
        /// <param name="dummyFramesNumber">The number of dummy frames to capture.
        /// Dummy frames are frames that are being captured but not saved in order to 'heat up' the camera. Default is <c>30</c>.</param>
        /// <param name="keepFrames">Whether to call <c>Keep</c> on the frames so they won't count towards the ObjectPool. Default is <c>true</c>.</param>
        /// <returns>An array of frames, whom have been captured by this <c>Camera</c>. </returns>
        /// <remarks>The caller need to dispose the frames.</remarks>
        /// <seealso cref="CaptureFrame(int, int)"/>
        public DepthFrame[] CaptureDepthFrames(int framesNumber = 1, int dummyFramesNumber = 30, bool keepFrames = true)
        {
            var config = GetDepthConfig();
            var pipe = new Pipeline();

            var pp = pipe.Start(config);
            try
            {
                for (var i = 0; i < dummyFramesNumber; ++i)
                {
                    using (var frames = pipe.WaitForFrames())
                    using (var depth = frames.DepthFrame) ;
                }

                var framesArr = new DepthFrame[framesNumber];

                for (var i = 0; i < framesNumber; ++i)
                {
                    using var frameset = pipe.WaitForFrames();
                    framesArr[i] = frameset.DepthFrame;
                    if (keepFrames)
                    {
                        framesArr[i].Keep();
                    }
                }

                return framesArr;
            }
            finally
            {
                pipe.Stop();
            }
        }

        /// <summary>
        /// Applies this camera's filters on <paramref name="frames"/>.
        /// </summary>
        /// <param name="frames">A collection of frames to 'average' and apply this camera's filters on.</param>
        /// <returns>The outcome of applying this camera's filters on <paramref name="frames"/>.</returns>
        /// <seealso cref="ApplyFilters(DepthFrame)"/>
        /// <remarks>The caller need to dispose the frame.</remarks>
        public DepthFrame ApplyFilters(IEnumerable<DepthFrame> frames)
        {

            var filters = GetOnFiltersInOrder();

            var i = 0;
            var j = 0;
            var curr = frames.FirstOrDefault();
            var prev = curr;
            foreach (var frame in frames)
            {
                j = 0;
                curr = frame.Clone().As<DepthFrame>();
                foreach (var filter in filters)
                {
                    prev = curr;
                    curr = filter.Process<DepthFrame>(prev);

                    prev.Dispose();
                    ++j;
                }
                ++i;
            }

            filters.ForEach(f => f.Dispose());

            return curr;
        }

        /// <summary>
        /// Adjust a point-cloud with this camera's deviations,
        /// changes to origin point to be relative to the center of the object (rather than relative to the camera),
        /// then rotates the point-cloud by this camera's angle.
        /// </summary>
        /// <param name="vertices">A point-cloud to apply this transformation on.</param>
        public void AdjustAndRotateInPlace(List<Vector3> vertices)
        {
            AdjustInPlace(vertices);
            RotateInPlace(vertices);
        }

        /// <summary>
        /// Adjust a point-cloud with this camera's deviations,
        /// changes to origin point to be relative to the center of the object (rather than relative to the camera)
        /// </summary>
        /// <param name="vertices">>A point-cloud to apply this transformation on.</param>
        public void AdjustInPlace(List<Vector3> vertices)
        {
            Utils.ChangeCoordinatesInPlace(vertices,
                v => new Vector3(-(v.X + PositionDeviation.X), -(v.Y + PositionDeviation.Y), PositionDeviation.Z - v.Z));

        }

        /// <summary>
        /// Rotates the point-cloud by this camera's angle.
        /// </summary>
        /// <param name="vertices">>A point-cloud to apply this transformation on.</param>
        public void RotateInPlace(List<Vector3> vertices)
        {
            Utils.RotateAroundYAxisInPlace(vertices, Angle);
        }

        /// <summary>
        /// Finds the angle where the 2 cameras FOVs meets, measured from 0,0 in relation to this camera.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>The angle that separates the 2 cameras FOVs.</returns>
        public float FindCriticalAngle(Camera other)
        {
            var halfPi = MathF.PI / 2;
            var d1 = PositionDeviation.Z;
            var fov1 = Utils.ToRadians(FOV.X / 2);
            var d2 = other.PositionDeviation.Z;
            var fov2 = Utils.ToRadians(other.FOV.X / 2);
            var deltaAngle = Utils.ToRadians(Angle - other.Angle);

            var x = (d2 * MathF.Sin(fov2) / MathF.Sin(deltaAngle + fov2) - d1) / (Utils.Cot(fov1) - MathF.Tan(halfPi + deltaAngle + fov2));
            var z = Utils.Cot(fov1) * x + d1;

            return Utils.ToDegrees(MathF.Atan(z / x));
        }
    }
}