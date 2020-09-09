using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using _3DScanning.ViewModel;
using Intel.RealSense;

namespace _3DScanning.View.Controls
{
    /// <summary>
    /// Interaction logic for UserControlLiveStream.xaml
    /// </summary>
    public partial class UserControlLiveStream : UserControl
    {
        private string SerialNum { get; set; }
        private Pipeline pipeline;
        private Colorizer colorizer;
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        public UserControlLiveStream(string serialNum)
        {
            InitializeComponent();
            SerialNum = serialNum;
        }

        static Action<VideoFrame> UpdateImage(Image img)
        {
            var wbmp = img.Source as WriteableBitmap;

            return new Action<VideoFrame>(frame =>
            {
                var rect = new Int32Rect(0, 0, (int)frame.Width, (int)frame.Height);
                wbmp.WritePixels(rect, frame.Data, frame.Stride * frame.Height, frame.Stride);
            });
        }

        private void SetupWindow(PipelineProfile pipelineProfile, out Action<VideoFrame> depth)
        {
            using (var p = pipelineProfile.GetStream(Stream.Depth).As<VideoStreamProfile>())
                img.Source = new WriteableBitmap(p.Width, p.Height, 96d, 96d, PixelFormats.Rgb24, null);
            depth = UpdateImage(img);
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as ToggleButton;
            if (btn.IsChecked != null)
            {
                if ((bool)btn.IsChecked)
                {
                    // run stream
                    img.Visibility = Visibility.Visible;
                    try
                    {
                        Action<VideoFrame> updateDepth;

                        // The colorizer processing block will be used to visualize the depth frames.
                        colorizer = new Colorizer();

                        // Create and config the pipeline to strem color and depth frames.
                        pipeline = new Pipeline();

                        var cfg = new Config();
                        cfg.EnableDevice(SerialNum);
                        cfg.EnableStream(Stream.Depth, 640, 480);
                        cfg.EnableStream(Stream.Color, Format.Rgb8);

                        var decimation = new DecimationFilter();

                        var pp = pipeline.Start(cfg);

                        SetupWindow(pp, out updateDepth);

                        Task.Run(() =>
                        {
                            try
                            {
                                while (!tokenSource.Token.IsCancellationRequested)
                                {
                                    // We wait for the next available FrameSet and using it as a releaser object that would track
                                    // all newly allocated .NET frames, and ensure deterministic finalization
                                    // at the end of scope. 
                                    using (var frames = pipeline.WaitForFrames())
                                    {
                                        var colorFrame = frames.ColorFrame.DisposeWith(frames);
                                        var depthFrame = frames.DepthFrame.DisposeWith(frames);



                                        // We colorize the depth frame for visualization purposes
                                        var colorizedDepth = colorizer.Process<VideoFrame>(depthFrame).DisposeWith(frames);

                                        // Render the frames.
                                        Dispatcher.Invoke(DispatcherPriority.Render, updateDepth, colorizedDepth);

                                        Dispatcher.Invoke(new Action(() =>
                                        {
                                            String depth_dev_sn = depthFrame.Sensor.Info[CameraInfo.SerialNumber];
                                        }));
                                    }
                                }
                            } catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                                btn.IsChecked = false;
                            }
                        }, tokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        btn.IsChecked = false;
                    }
                }
                else if (!(bool)btn.IsChecked)
                {
                    // close stream
                    tokenSource.Cancel();
                    pipeline.Stop();

                }
            }

        }
    }
}
