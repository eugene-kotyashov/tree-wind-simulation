using System.Windows.Media.Media3D;

namespace WpfApp4.Physics
{
    public class Wind
    {
        public Vector3D Direction { get; set; }
        public double Strength { get; set; }
        public double Turbulence { get; set; }
        private Random random = new Random();
        public double PeriodSeconds { get; set; }

        public Wind(Vector3D direction, double strength, double period)
        {
            Direction = direction;
            Direction.Normalize();
            Strength = strength;
            this.PeriodSeconds = period;
        }

        public Vector3D GetForce( double time)
        {
            return Direction * Strength*(1.0 + Math.Sin(2*Math.PI*time/PeriodSeconds));
        }
    }
} 