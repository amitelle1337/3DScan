using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using _3DScan.ViewModel;

namespace _3DScan.View.Controls
{
    /// <summary>
    /// Interaction logic for UserControlCameraView.xaml
    /// </summary>
    public partial class UserControlCameraView : UserControl
    {
        public UserControlCameraView(CamerasManagerVM.Camera cam)
        {
            InitializeComponent();
            this.DataContext = cam;
            container.Children.Add(new UserControlLiveStream(cam.Serial));
        }

        private void Grid_GotFocus(object sender, RoutedEventArgs e)
        {
            var converter = new BrushConverter();
            OuterBrush.BorderBrush = (Brush)converter.ConvertFromString("#3891A6");
        }

        private void Grid_LostFocus(object sender, RoutedEventArgs e)
        {
            var converter = new BrushConverter();
            OuterBrush.BorderBrush = (Brush)converter.ConvertFromString("gray");
        }
    }
}
