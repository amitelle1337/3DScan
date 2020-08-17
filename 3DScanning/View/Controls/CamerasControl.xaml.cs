using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Converters;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using _3DScanning.ViewModel;

namespace _3DScanning.View.Controls
{
    /// <summary>
    /// Interaction logic for CamerasControl.xaml
    /// </summary>
    public partial class CamerasControl : UserControl
    {
        public CamerasControl()
        {
            InitializeComponent();
            var cm = (CamerasManager)App.Current.Properties["CamerasManager"];
            var cams = cm.Cameras;

            var items = new List<UserControlCameraView>();

            foreach (CamerasManager.Camera c in cams)
            {
                items.Add(new UserControlCameraView(c));
            }

            CameraViews.ItemsSource = items;
        }
    }
}
