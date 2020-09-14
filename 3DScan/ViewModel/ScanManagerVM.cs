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
        private ScanModel Model1 { get; set; }

        public ScanManagerVM(ScanModel model)
        {
            this.Model1 = model;

        }

        public void Capture()
        {
            //Model.SavePointCloud(Model.ScanObject(), "xyz");
        }

        public void Calibrate()
        {
            //Model.Calibrate();
        }
    }
}
