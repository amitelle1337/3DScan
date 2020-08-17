using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace _3DScanning.Model
{
    public static class Utils
    {
        /// <summary>
        /// Changes to coordinate of each vector in <paramref name="vertices"/> according to the result of <paramref name="func"/> on that vector.
        /// </summary>
        /// <param name="vertices">The list of vectors to change.</param>
        /// <param name="func">The change function to perform on each vector.</param>
        public static void ChangeCoordinatesInPlace(List<Vector3> vertices, Func<Vector3, Vector3> func)
        {
            for (var i = 0; i < vertices.Count; ++i)
            {
                vertices[i] = func(vertices[i]);
            }
        }

        /// <summary>
        /// Converts a DepthFrame to a point-cloud with none-zero points.
        /// </summary>
        /// <param name="frame">A frame to convert to a point-cloud.</param>
        /// <returns>The resulted point-cloud generated from that frame.</returns>
        public static List<Vector3> FrameToPointCloud(DepthFrame frame)
        {
            var pc = new PointCloud();

            using var points = pc.Process(frame).As<Points>();
            var tmp = new Vector3[points.Count];
            points.CopyVertices(tmp);

            var vertices = new List<Vector3>();


            foreach (var v in tmp)
            {
                if (!v.Equals(Vector3.Zero))
                {
                    vertices.Add(v);
                }
            }

            return vertices;
        }

        /// <summary>
        /// Rotates a point-cloud around the y axis <paramref name="angle"/> degrees.
        /// </summary>
        /// <param name="vertices">The list of vectors to rotated around the y axis.</param>
        /// <param name="angle">The angle, measured in degrees.</param>
        public static void RotateAroundYAxisInPlace(List<Vector3> vertices, double angle)
        {
            var radAngle = ToRadians(angle);
            var rotationMatrix = new Matrix4x4(
                (float)Math.Cos(radAngle), 0, -(float)Math.Sin(radAngle), 0,
                0, 1, 0, 0,
                (float)Math.Sin(radAngle), 0, (float)Math.Cos(radAngle), 0,
                0, 0, 0, 1
            );

            for (var i = 0; i < vertices.Count; ++i)
            {
                vertices[i] = Vector3.Transform(vertices[i], rotationMatrix);
            }
        }

        /// <summary>
        /// Writes a .xyz file with the <paramref name="vertices"/>.
        /// </summary>
        /// <param name="filename">The name of the file to write.</param>
        /// <param name="vertices">The points to write to the file.</param>
        public static void WriteXyz(string filename, IEnumerable<Vector3> vertices)
        {
            using var writer = new StreamWriter(filename);
            foreach (var v in vertices)
            {
                writer.WriteLine($"{v.X} {v.Y} {v.Z}");
            }
        }

        /// <summary>
        /// Disposes each frame in <paramref name="frames"/>
        /// </summary>
        /// <param name="frames">The frames to dispose.</param>
        public static void DisposeAll(IEnumerable<Frame> frames)
        {
            // Append all frames to be dispose with the releaser and dispose them at once
            using var releaser = new FramesReleaser();
            foreach (var frame in frames)
            {
                frame.DisposeWith(releaser);
            }
        }

        /// <summary>
        /// Averages a collections of vectors.
        /// </summary>
        /// <param name="vertices">The vectors to find the average of.</param>
        /// <returns>The average of the vectors in <paramref name="vertices"/></returns>
        public static Vector3 Average(IEnumerable<Vector3> vertices)
        {
            var count = 0;
            Vector3 acum = Vector3.Zero;

            // Maybe using .Average on each dimension is faster (Maybe even in parallel :O)
            foreach (var v in vertices)
            {
                acum += v;
                ++count;
            }

            return acum / count;
        }

        /// <summary>
        /// Converts and angle,measured in degrees, to radians.
        /// </summary>
        /// <param name="angle">The angle, measured in degrees.</param>
        /// <returns>The <paramref name="angle"/> in radians.</returns>
        public static double ToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }
    }
}
