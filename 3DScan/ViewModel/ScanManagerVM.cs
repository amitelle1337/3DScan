using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using _3DScan.Model;

namespace _3DScan.ViewModel
{
    class ScanManagerVM
    {
        private ScanManager Model { get; set; }

        public ScanManagerVM()
        {

            //using (StreamReader r = new StreamReader("config.json"))
            //{
            //    var jsonString = r.ReadToEnd();
            //    Model = JsonSerializer.Deserialize<ScanManager>(jsonString);
            //}
            Model = new ScanManager(new Intel.RealSense.Context());

        }

        public void Capture()
        {
            Model.SavePointCloud(Model.ScanObject(), "xyz");
        }

        public void Calibrate()
        {
            Model.Calibrate();
        }
    }
}
