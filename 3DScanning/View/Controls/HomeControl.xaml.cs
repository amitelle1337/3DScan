using _3DScanning.ViewModel;
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

namespace _3DScanning.View.Controls
{
    /// <summary>
    /// Interaction logic for HomeControl.xaml
    /// </summary>
    public partial class HomeControl : UserControl
    {
        public HomeControl()
        {
            InitializeComponent();

            var vm = (CamerasManager)App.Current.Properties["CamerasManager"];
            LeftSide.Children.Add(new UserControlCamerasStructure(vm.Locations));

            // initialize left side
            // Capture button
            // file choice
            // Calibrate button
        }
    }
}
