using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _3DScanning.View.Controls
{
    /// <summary>
    /// Interaction logic for UserControlCamerasStructure.xaml
    /// </summary>
    public partial class UserControlCamerasStructure : UserControl
    {
        public UserControlCamerasStructure(List<(String label, (double x, double y))> points)
        {
            InitializeComponent();

            var locations = new List<(double x, double y)>();
            foreach ((string _, (double x, double y)) in points)
            {
                locations.Add((x, y));
            }

            // Scale everything
            var maximum = max(locations);
            var minimum = min(locations);

            double height = 250, width = 250;

            // normalzation:
            for (var i = 0; i < locations.Count; i++)
            {
                var a = width * (locations[i].x - minimum.Item1) / (maximum.Item1 - minimum.Item1);
                var b = height * (locations[i].y - minimum.Item2) / (maximum.Item2 - minimum.Item2);
                locations[i] = (a, b);
                points[i] = (points[i].label ,(a, b));
            }

            // draw:
            foreach ((string label, (double x, double y)) in points)
            {
                var myEllipse = new Ellipse();
                var b = PickBrush();
                myEllipse.Fill = b;
                myEllipse.StrokeThickness = 0.5;
                myEllipse.Stroke = Brushes.Black;
                myEllipse.Width = 10;
                myEllipse.Height = 10;
                Canvas.SetLeft(myEllipse, x);
                Canvas.SetTop(myEllipse, y);
                myCanvas.Children.Add(myEllipse);

                var textblock = new TextBlock();
                textblock.Text = label.Substring(label.Length - 5);
                Canvas.SetLeft(textblock, x + 5);
                Canvas.SetTop(textblock, y + 5);
                myCanvas.Children.Add(textblock);
            }
        }

        private static (double, double) max(List<(double x, double y)> l)
        {
            double maxX = l[0].x, maxY = l[0].x;

            foreach ((double a, double b) in l)
            {
                if (a > maxX)
                    maxX = a;
                if (b > maxY)
                    maxY = b;
            }

            return (maxX, maxY);
        }

        private static (double, double) min(List<(double x, double y)> l)
        {
            double minX = l[0].y, minY = l[0].y;

            foreach ((double a, double b) in l)
            {
                if (a < minX)
                    minX = a;
                if (b < minY)
                    minY = b;
            }

            return (minX, minY);
        }

        private static Brush PickBrush()
        {

            var rnd = new Random();
            var brushesType = typeof(Brushes);
            PropertyInfo[] properties = brushesType.GetProperties();

            int random = rnd.Next(properties.Length);
            return (Brush)properties[random].GetValue(null, null);
        }
    }
}
