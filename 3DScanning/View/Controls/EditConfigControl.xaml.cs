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
using _3DScanning.ViewModel;

namespace _3DScanning.View.Controls
{
    /// <summary>
    /// Interaction logic for EditConfigControl.xaml
    /// </summary>
    public partial class EditConfigControl : UserControl
    {
        public EditConfigControl()
        {
            InitializeComponent();

            var items = new List<UserControlCameraEdit>();

            var vm = (CamerasManagerVM)Application.Current.Properties["CamerasManagerVM"];

            foreach (CamerasManagerVM.Camera c in vm.Cameras)
            {
                var item = new UserControlCameraEdit(c);
                items.Add(item);
            }

            EditForms.ItemsSource = items;
        }
    }
}
