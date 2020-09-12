using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace _3DScan.Model
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
        /// Rotates a point-cloud around the y axis <paramref name="angle"/> degrees counter-clockwise.
        /// </summary>
        /// <param name="vertices">The list of vectors to rotated around the y axis.</param>
        /// <param name="angle">The angle, measured in degrees.</param>
        public static void RotateAroundYAxisInPlace(List<Vector3> vertices, float angle)
        {
            var radAngle = ToRadians(angle);
            var rotationMatrix = new Matrix4x4(
                MathF.Cos(radAngle), 0, MathF.Sin(radAngle), 0,
                0, 1, 0, 0,
                -MathF.Sin(radAngle), 0, MathF.Cos(radAngle), 0,
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
        public static void WriteXYZ(string filename, IEnumerable<Vector3> vertices)
        {
            WriteXYZAsync(filename, vertices).GetAwaiter().GetResult();
        }

        /// <summary>
        /// An async version of <see cref="WriteXYZ(string, IEnumerable{Vector3})"/>.
        /// </summary>
        /// <param name="filename">The name of the file to write.</param>
        /// <param name="vertices">The points to write to the file.</param>
        /// <returns></returns>
        public static async Task WriteXYZAsync(string filename, IEnumerable<Vector3> vertices)
        {
            using var writer = new StreamWriter(filename);

            // The number of vectors is typically very large, waiting for each line to written wasteful, thus we use a string builder to build the string, and then write it.
            // The recommended buffer size for a float is 16 byte, plus one extra byte for each float, times the number of floats
            var builder = new StringBuilder(vertices.Count() * 3 * (16 + 1));
            foreach (var v in vertices)
            {
                builder.AppendLine($"{v.X} {v.Y} {v.Z}");
            }
            await writer.WriteAsync(builder).ConfigureAwait(false);
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
        public static float ToRadians(float angle)
        {
            return (MathF.PI / 180) * angle;
        }

        /// <summary>
        /// Converts and angle,measured in radians, to degrees.
        /// </summary>
        /// <param name="angle">The angle, measured in radians.</param>
        /// <returns>The <paramref name="angle"/> in degrees.</returns>
        public static float ToDegrees(float angle)
        {
            return (180 / MathF.PI) * angle;
        }

        /// <summary>
        /// Calculates the cotangent of <paramref name="angle"/>.
        /// </summary>
        /// <param name="angle">The angle, measured in radians.</param>
        /// <returns>The cotangent of <paramref name="angle"/>.</returns>
        public static float Cot(float angle)
        {
            return 1 / MathF.Tan(angle);
        }

    }
}
