using HelixToolkit.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ThrusterVisualizer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AddThrusters();
        }

        private void AddThrusters()
        {
            double r = 1;
            Point3D[] positions = new Point3D[]
            {
                new Point3D(r,r,r),
                new Point3D(r,r,-r),
                new Point3D(r,-r,r),
                new Point3D(r,-r,-r),
                new Point3D(-r,r,r),
                new Point3D(-r,r,-r)
            };

            Vector3D[] directions = new Vector3D[]
            {
                new Vector3D(1,1,1),
                new Vector3D(1,1,-1),
                new Vector3D(1,-1,1),
                new Vector3D(1,-1,-1),
                new Vector3D(-1,1,1),
                new Vector3D(-1,1,-1)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                // Sphere for thruster
                var sphere = new SphereVisual3D
                {
                    Center = positions[i],
                    Radius = 0.05,
                    Fill = Brushes.Red
                };
                viewport.Children.Add(sphere);

                // Arrow for direction
                var arrow = new ArrowVisual3D
                {
                    Point1 = positions[i],
                    Point2 = positions[i] + directions[i] * 0.5,
                    Diameter = 0.02,
                    Fill = Brushes.Blue
                };
                viewport.Children.Add(arrow);
            }
        }
    }
}
