namespace _3DScan.Model
{
    /// <summary>
    /// Represents the technology the <c>Camera</c> uses.
    /// </summary>
    /// <see cref="Camera"/>
    public enum CameraType : ushort
    {
        /// <summary>
        /// The <c>Undefined</c> value represents a camera that the current version of the code does not support.
        /// </summary>
        Undefined,
        /// <summary>
        /// The <c>Sereo_Depth</c> value represents stereo-depth technology cameras (e.g. D435i, D415, etc.).
        /// </summary>
        Sereo_Depth,
        /// <summary>
        /// The <c>LiDAR</c> value represents LiDAR technology cameras (e.g. L515.).
        /// </summary>
        LiDAR,
        /// <summary>
        /// The <c>Coded_Light</c> value represents coded-light cameras (e.g. SR305).
        /// </summary>
        Coded_Light
    }
}
