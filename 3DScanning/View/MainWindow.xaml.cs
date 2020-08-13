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
using _3DScanning.ViewModel;
using _3DScanning.View.Controls;

namespace _3DScanning
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var item0 = new ItemMenu("Home", new UserControl(), PackIconKind.ViewDashboard);
 
            var menuConfig = new List<SubItem>();
            menuConfig.Add(new SubItem("Edit Configurations"));
            menuConfig.Add(new SubItem("Load Configurations"));
            menuConfig.Add(new SubItem("Save Configurations"));
            var item1 = new ItemMenu("Configurations", menuConfig, PackIconKind.FileReport);

            var menuRegister = new List<SubItem>();
            menuRegister.Add(new SubItem("View Cameras"));
            menuRegister.Add(new SubItem("Reload"));
            var item2 = new ItemMenu("Cameras", menuRegister, PackIconKind.Register);

            var item3 = new ItemMenu("About", new UserControl(), PackIconKind.ViewDashboard);

            Menu.Children.Add(new UserControlMenuItem(item0));
            Menu.Children.Add(new UserControlMenuItem(item1));
            Menu.Children.Add(new UserControlMenuItem(item2));
            Menu.Children.Add(new UserControlMenuItem(item3));
        }
    }
}
