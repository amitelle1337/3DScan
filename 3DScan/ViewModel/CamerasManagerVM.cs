using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using _3DScan.Model;

namespace _3DScan.ViewModel
{
    public class CamerasManagerVM
    {

        private ScanModel Model {get;}
        public List<Camera> Cameras { get; private set; }
        public List<String> SequenceNumbers { 
            get {
                var l = new List<String>();
                foreach (Camera c in Cameras)
                {
                    l.Add(c.Serial);
                }
                return l;
            } }
        public List<(String serial, (double x, double y))> Locations { 
            get
            {
                var l = new List<(String serial, (double x, double y))>();
                foreach (Camera c in Cameras) 
                {
                    var r = 2 * c.Position.Z * Math.Sin(ToRadians(c.Angle / 2));
                    var a = r * Math.Cos(ToRadians(c.Angle / 2));
                    var b = r * Math.Sin(ToRadians(c.Angle / 2));
                    l.Add((c.Serial, (a, b)));
                }
                return l;
            } }
        Model.ScanManager ScanManagerModel { get; set; }

        public CamerasManagerVM(ScanModel model)
        {
            ScanManagerModel = new Model.ScanManager();
            Model = model;
            var l = new List<Camera>();

            foreach(var cam in Model.Cameras)
            {
                l.Add(new Camera(cam.Serial, cam.Angle, cam.PositionDeviation, cam.On));
            }
            
            Cameras = l;
        }

        public class Camera
        {
            public string Serial { get; set; }
            public float Angle { get; set; }

            public Vector3 Position { get; set; }
            public bool On { get; set; }

            public Camera(String serial)
            {
                Serial = serial;
                Angle = -1;
                Position = new Vector3(0, 0, 0);
                On = false;
            }

            public Camera(String serial, float angle, Vector3 pos, bool on)
            {
                Serial = serial;
                Angle = angle;
                Position = new Vector3(pos.X, pos.Y, pos.Z);
                On = on;
            }
        }

        public static double ToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

    }
}
