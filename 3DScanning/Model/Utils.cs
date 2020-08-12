using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace _3DScanning.Model
{
    public static class Utils
    {
        public static void ChangeCoordinatesInPlace(List<Vector3> vertices, Func<Vector3, Vector3> change)
        {
            for (var i = 0; i < vertices.Count; ++i)
            {
                vertices[i] = change(vertices[i]);
            }
        }

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

        public static void RotatePointCloudInPlace(List<Vector3> vertices, float angle)
        {
            var radAngle = angle.ToRadians();
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


        public static void WriteXYZ(string filename, IEnumerable<Vector3> vertices)
        {
            using var writer = new StreamWriter(filename);
            foreach (var v in vertices)
            {
                writer.WriteLine($"{v.X} {v.Y} {v.Z}");
            }
        }

        public static void DisposeAll(IEnumerable<Frame> frames)
        {
            // Append all frames to be dispose with the releaser and dispose them at once
            using var releaser = new FramesReleaser();
            foreach (var frame in frames)
            {
                frame.DisposeWith(releaser);
            }
        }
    }


    public static class NumericExtensions
    {
        public static double ToRadians(this double val)
        {
            return (Math.PI / 180) * val;
        }

        public static double ToRadians(this float val)
        {
            return ((double)val).ToRadians();
        }
    }
}
