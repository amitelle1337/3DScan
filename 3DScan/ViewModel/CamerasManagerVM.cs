using System;
using System.Collections.Generic;
using System.Text;
using _3DScan.Model;

namespace _3DScan.ViewModel
{
    public class CamerasManagerVM
    {
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
                    var r = 2 * c.Distance * Math.Sin(ToRadians(c.Angle / 2));
                    var a = r * Math.Cos(ToRadians(c.Angle / 2));
                    var b = r * Math.Sin(ToRadians(c.Angle / 2));
                    l.Add((c.Serial, (a, b)));
                }
                return l;
            } }
        Model.ScanManager ScanManagerModel { get; set; }

        public CamerasManagerVM()
        {
            ScanManagerModel = new Model.ScanManager();

            var l = new List<Camera>
            {
                new Camera("918512070565", 0.7f, 0, 0, 0, 0, true),
                new Camera("00000000f0141013", 0.68f, 60, 0, 0, 0, true),
                new Camera("918512073384", 0.75f, 120, 0, 0, 0, true),
                new Camera("00000000f0090129", 0.71f, 180, 0, 0, 0, true),
                new Camera("918512072325", 0.7f, 240, 0, 0, 0, true),
                new Camera("00000000f0090440", 0.72f, 300, 0, 0, 0, true)
            };
            Cameras = l;
        }

        public class Camera
        {
            public string Serial { get; set; }
            public float Distance { get; set; }
            public float Angle { get; set; }
            public float Dx { get; set; }
            public float Dy { get; set; }
            public float Dz { get; set; }
            public bool On { get; set; }

            public Camera(String serial)
            {
                Serial = serial;
                Distance = -1;
                Angle = -1;
                Dx = Dy = Dz = 0;
                On = false;
            }

            public Camera(String serial, float distance, float angle, float dx, float dy, float dz, bool on)
            {
                Serial = serial;
                Distance = distance;
                Angle = angle;
                Dx = dx;
                Dy = dy;
                Dz = dz;
                On = on;
            }
        }

        public static double ToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

    }
}
