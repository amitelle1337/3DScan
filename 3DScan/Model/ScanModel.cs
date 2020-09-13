using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace _3DScan.Model
{
    /// <summary>
    /// Class <c>ScanModel</c> creates a facade for the scanner's business logic.
    /// </summary>
    public class ScanModel
    {
        public ScanManager Manager { get; private set; }
        public List<Camera> Cameras { get { return this.Manager.Cameras;} }


        /// <summary>
        /// Initializes a default <c>ScanModel</c> including the connected cameras.
        /// If given, the model adapts itself to an existing configuration file.
        /// </summary>
        /// <param name="filename">A configuration file name (if one exists).</param>
        public ScanModel(string filename = null)
        {
            if (filename == null)
                this.Manager = new ScanManager(new Intel.RealSense.Context());
            else
            {
                this.FileUpdate(filename);
            }
        }

        /// <summary>
        /// Sets configurations from existing file.
        /// </summary>
        /// <param name="filename">A configuration file name.</param>
        public void FileUpdate(String filename)
        {
            try
            {
                using (StreamReader r = new StreamReader(filename))
                {
                    var jsonString = r.ReadToEnd();
                    this.Manager = JsonSerializer.Deserialize<ScanManager>(jsonString);
                }
            }
            catch
            {
                throw new Exception("Error loading configuration file.\n\"{filename}\" might be invalid.");
            }
        }

        /// <summary>
        /// Clears all configurations and sets default values.
        /// </summary>
        public void Clear()
        {
            this.Manager = new ScanManager(new Intel.RealSense.Context());
        }

    }
}
