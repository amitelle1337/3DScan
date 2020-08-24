﻿using Intel.RealSense;
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
        /// <remarks>The value of <c>Type</c> and <c>vFOV</c> is automatically updated when this value is changed.</remarks>
        /// <seealso cref="Type"/>
        /// <seealso cref="FOV"/>
        public string Serial
        {
            get => _serial;
            set
            {
                _serial = value;
                Type = CameraTypeFunctions.QuarryCameraType(_serial);
                var intrinsics = GetDepthIntrinsics();
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

        public DecimationFilterWarpper DecimationWrapper { get; private set; }
        public SpatialFilterWarpper SpatialWrapper { get; private set; }
        public TemporalFilterWrapper TemporalWrapper { get; private set; }
        public HoleFillingFilterWrapper HoleFillingWrapper { get; private set; }
        public ThresholdFilterWrapper ThresholdWrapper { get; private set; }

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
        public FilterWrapper[] GetWrapperInOrder()
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

            foreach (var wrapper in GetWrapperInOrder())
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
        /// <param name="framesNumber">The number of frames to capture.</param>
        /// <param name="dummyFramesNumber">The number of dummy frames to capture. Dummy frames are frames that are being captured but not saved in order to 'heat up' the camera.</param>
        /// <returns>An array of frames, whom have been captured by this <c>Camera</c>. </returns>
        /// <remarks>The caller need to dispose the frames.</remarks>
        /// <seealso cref="CaptureFrame(int, int)"/>
        public DepthFrame[] CaptureFrames(int framesNumber = 1, int dummyFramesNumber = 30)
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
                }

                return framesArr;
            }
            finally
            {
                pipe.Stop();
            }
        }

        /// <summary>
        /// Captures an average of frame from this <c>Camera</c>. (Capturing several frames and applying temporal filter).
        /// </summary>
        /// <param name="framesNumber">The number of frames to capture.</param>
        /// <param name="dummyFramesNumber">The number of dummy frames to capture. Dummy frames are frames that are being captured but not saved in order to 'heat up' the camera.</param>
        /// <returns>A of frame, whom has been the result of applying a temporal filter on frames that where capture by this <c>Camera</c>. </returns>
        /// <seealso cref="CaptureFrames(int, int)"/>
        /// <remarks>
        /// The caller need to dispose the frame. <br>
        /// Practically the same as the previous method, but uses temporal filter during capture to preserve memory and 'stop-the-world' pauses when disposing all frames at once.
        /// </remarks>
        /// 
        public DepthFrame CaptureFrame(int framesNumber = 1, int dummyFramesNumber = 30)
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
                using var firstFrameset = pipe.WaitForFrames();
                var frame = firstFrameset.DepthFrame;
                using (var temporalFilter = new TemporalFilter())
                {
                    // Release temporary frames
                    using var realeser = new FramesReleaser();

                    for (var i = 0; i < framesNumber - 1; ++i)
                    {
                        if (i < framesNumber - 1)
                        {
                            frame.DisposeWith(realeser);
                        }

                        using var frameset = pipe.WaitForFrames();
                        using var depth = frameset.DepthFrame;
                        frame = temporalFilter.Process<DepthFrame>(depth);
                    }

                    return frame;
                }

            }
            finally
            {
                pipe.Stop();
            }
        }

        /// <summary>
        /// Applies this camera's filters on the resulted frame after 'averaging' them with temporal filter.
        /// </summary>
        /// <param name="frames">A collection of frames to 'average' and apply this camera's filters on.</param>
        /// <returns>The outcome of applying a temporal filter on frames and then applying this camera's filters on the resulted frame.</returns>
        /// <seealso cref="ApplyFilters(DepthFrame)"/>
        /// <remarks>The caller need to dispose the frame.</remarks>
        public DepthFrame ApplyFilters(IEnumerable<DepthFrame> frames)
        {
            var res = frames.First(_ => true);
            using (var tempFilter = new TemporalFilter())
            {
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
        }

        /// <summary>
        /// Applies this camera's filters on the frame.
        /// </summary>
        /// <param name="frame">A frame to apply this camera's filters on.</param>
        /// <returns>The outcome of applying this camera's filters on the frame.</returns>
        /// <seealso cref="ApplyFilters(IEnumerable{DepthFrame})"/>
        /// <remarks>The caller need to dispose the frame.</remarks>
        public DepthFrame ApplyFilters(DepthFrame frame)
        {
            var res = frame.Clone().As<DepthFrame>();

            var filters = GetOnFiltersInOrder();

            var i = 0;
            foreach (var filter in filters)
            {
                res = filter.Process<DepthFrame>(res);

                // Release each frame which is not the last
                if (i < filters.Count() - 1)
                {
                    res.Dispose();
                }
                filter.Dispose();
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