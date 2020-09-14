using System;
using System.Collections.Generic;
using MaterialDesignThemes.Wpf;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using _3DScan.View.Controls;
using _3DScan.View;
using _3DScan.Model;

namespace _3DScan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<String, UserControl> controls;

        public MainWindow()
        {
            var m = new ScanModel();
            Application.Current.Properties["Model"] = m;
            Application.Current.Properties["CamerasManagerVM"] = new CamerasManagerVM(m);
            Application.Current.Properties["ScanManagerVM"] = new ScanManagerVM(m);
            InitializeComponent();
            
            controls = new Dictionary<string, UserControl>();

            var item0 = new ItemMenu("Home", new HomeControl(), PackIconKind.ViewDashboard);
            controls.Add("Home", new HomeControl());

            var menuConfig = new List<SubItem>();
            menuConfig.Add(new SubItem("Edit Configurations", new EditConfigControl()));
            menuConfig.Add(new SubItem("Load Configurations", new LoadConfigControl()));
            menuConfig.Add(new SubItem("Save Configurations", new SaveConfigControl()));
            var item1 = new ItemMenu("Configurations", menuConfig, PackIconKind.FileReport);

            var menuRegister = new List<SubItem>();
            menuRegister.Add(new SubItem("View Cameras", new CamerasControl()));
            menuRegister.Add(new SubItem("Reload"));
            var item2 = new ItemMenu("Cameras", menuRegister, PackIconKind.Register);

            var item3 = new ItemMenu("About", new UserControl(), PackIconKind.ViewDashboard);
            controls.Add("About", new AboutControl());

            Menu.Children.Add(new UserControlMenuItem(item0, this));
            Menu.Children.Add(new UserControlMenuItem(item1, this));
            Menu.Children.Add(new UserControlMenuItem(item2, this));
            Menu.Children.Add(new UserControlMenuItem(item3, this));

            this.SwitchPage("Home");
        }

        internal void SwitchPage(UserControl screen)
        {
            if (screen != null) 
            {
                MainPage.Children.Clear();
                MainPage.Children.Add(screen);
            }
        }

        internal void SwitchPage(String name)
        {
            if (controls.ContainsKey(name)) 
            {
                MainPage.Children.Clear();
                MainPage.Children.Add(controls[name]);
            }
        }
    }
}
