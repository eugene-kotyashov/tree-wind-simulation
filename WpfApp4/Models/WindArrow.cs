using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WpfApp4.Models
{
    public class WindArrow
    {
        public static Model3DGroup CreateArrow(Point3D position, Vector3D direction, double size, Color color)
        {
            var result = new Model3DGroup();
            direction.Normalize();

            // Create shaft
            var shaft = CreateCylinder(position, direction * size * 0.8, size * 0.05);
            
            // Create arrowhead
            var arrowheadPosition = position + direction * size * 0.8;
            var arrowhead = CreateCone(arrowheadPosition, direction * size * 0.2, size * 0.15);

            // Set materials
            var material = new DiffuseMaterial(new SolidColorBrush(color));
            shaft.Material = material;
            shaft.BackMaterial = material;
            arrowhead.Material = material;
            arrowhead.BackMaterial = material;

            result.Children.Add(shaft);
            result.Children.Add(arrowhead);

            return result;
        }

        private static GeometryModel3D CreateCylinder(Point3D start, Vector3D direction, double radius)
        {
            var mesh = new MeshGeometry3D();
            int segments = 12;

            // Calculate orientation vectors
            Vector3D up = new Vector3D(0, 1, 0);
            if (Math.Abs(Vector3D.DotProduct(direction, up)) > 0.9)
            {
                up = new Vector3D(1, 0, 0);
            }
            Vector3D right = Vector3D.CrossProduct(direction, up);
            right.Normalize();
            up = Vector3D.CrossProduct(right, direction);
            up.Normalize();

            // Create vertices
            for (int i = 0; i <= segments; i++)
            {
                double angle = (2 * Math.PI * i) / segments;
                Vector3D offset = right * Math.Cos(angle) * radius + up * Math.Sin(angle) * radius;
                mesh.Positions.Add(start + offset);
                mesh.Positions.Add(start + direction + offset);
            }

            // Create triangles
            for (int i = 0; i < segments; i++)
            {
                int i0 = i * 2;
                int i1 = i * 2 + 1;
                int i2 = (i + 1) * 2;
                int i3 = (i + 1) * 2 + 1;

                mesh.TriangleIndices.Add(i0);
                mesh.TriangleIndices.Add(i1);
                mesh.TriangleIndices.Add(i2);

                mesh.TriangleIndices.Add(i2);
                mesh.TriangleIndices.Add(i1);
                mesh.TriangleIndices.Add(i3);
            }

            return new GeometryModel3D { Geometry = mesh };
        }

        private static GeometryModel3D CreateCone(Point3D apex, Vector3D direction, double radius)
        {
            var mesh = new MeshGeometry3D();
            int segments = 12;

            // Calculate orientation vectors (same as cylinder)
            Vector3D up = new Vector3D(0, 1, 0);
            if (Math.Abs(Vector3D.DotProduct(direction, up)) > 0.9)
            {
                up = new Vector3D(1, 0, 0);
            }
            Vector3D right = Vector3D.CrossProduct(direction, up);
            right.Normalize();
            up = Vector3D.CrossProduct(right, direction);
            up.Normalize();

            // Add apex point
            mesh.Positions.Add(apex);

            // Create base vertices
            Point3D baseCenter = apex - direction;
            for (int i = 0; i <= segments; i++)
            {
                double angle = (2 * Math.PI * i) / segments;
                Vector3D offset = right * Math.Cos(angle) * radius + up * Math.Sin(angle) * radius;
                mesh.Positions.Add(baseCenter + offset);
            }

            // Create triangles
            for (int i = 1; i < segments; i++)
            {
                // Side face
                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(i);
                mesh.TriangleIndices.Add(i + 1);

                // Base triangle
                mesh.TriangleIndices.Add(1);
                mesh.TriangleIndices.Add(i + 1);
                mesh.TriangleIndices.Add(i);
            }

            return new GeometryModel3D { Geometry = mesh };
        }

        public static void UpdateArrow(Model3DGroup arrow, Vector3D direction, Point3D position)
        {
            var transform = new Transform3DGroup();
            
            // Calculate rotation
            var currentDir = new Vector3D(1, 0, 0); // Default direction
            var rotationAxis = Vector3D.CrossProduct(currentDir, direction);
            if (rotationAxis.Length > 0.001)
            {
                double angle = Vector3D.AngleBetween(currentDir, direction);
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(rotationAxis, angle)));
            }

            // Add translation
            transform.Children.Add(new TranslateTransform3D(position.X, position.Y, position.Z));
            
            arrow.Transform = transform;
        }
    }
} 