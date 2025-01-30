using System.Windows.Media.Media3D;

namespace WpfApp4.Physics
{
    public class Wind
    {
        public Vector3D Direction { get; set; }
        public double Strength { get; set; }
        public double Turbulence { get; set; }
        private Random random = new Random();

        public Wind(Vector3D direction, double strength)
        {
            Direction = direction;
            Direction.Normalize();
            Strength = strength;
        }

        public Vector3D GetForce( double time)
        {
            return Direction * Strength;
        }
    }
} 