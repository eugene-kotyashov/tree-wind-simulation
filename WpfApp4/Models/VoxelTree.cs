using System.Windows.Media.Media3D;
using System.Windows.Media;
using HelixToolkit.Wpf;

namespace WpfApp4.Models
{
    public class VoxelTree
    {
        private class Voxel
        {
            public Point3D Position { get; set; }
            public Color Color { get; set; }
            public double Size { get; set; }
            public int Level { get; set; } // Used for wind effect
        }

        private List<Voxel> voxels = new();
        private MeshGeometry3D treeMesh;
        private Point3DCollection originalVertices;
        private readonly Dictionary<GeometryModel3D, Point3DCollection> originalPositions;
        private Random random = new Random(42);

        public VoxelTree(Point3D position, double height, double width, 
            Dictionary<GeometryModel3D, Point3DCollection> originalPositions)
        {
            this.originalPositions = originalPositions;
            GenerateVoxelStructure(position, height, width);
            GenerateTreeMesh();
        }

        private void GenerateVoxelStructure(Point3D basePosition, double height, double width)
        {
            double voxelSize = width / 5;
            int trunkHeight = (int)(height / voxelSize);
            int leafRadius = (int)(width / voxelSize);

            // Generate trunk
            for (int y = 0; y < trunkHeight; y++)
            {
                voxels.Add(new Voxel
                {
                    Position = new Point3D(
                        basePosition.X,
                        basePosition.Y + y * voxelSize,
                        basePosition.Z
                    ),
                    Color = Colors.SaddleBrown,
                    Size = voxelSize,
                    Level = 0
                });
            }

            // Generate leaf clusters
            int leafLevels = 4;
            for (int level = 0; level < leafLevels; level++)
            {
                double y = basePosition.Y + (trunkHeight - leafLevels + level) * voxelSize;
                int currentRadius = leafRadius - level / 2;

                for (int x = -currentRadius; x <= currentRadius; x++)
                {
                    for (int z = -currentRadius; z <= currentRadius; z++)
                    {
                        // Create spherical-like shape
                        double distance = Math.Sqrt(x * x + z * z);
                        if (distance <= currentRadius)
                        {
                            // Add some randomness to make it look more natural
                            if (random.NextDouble() > 0.3)
                            {
                                voxels.Add(new Voxel
                                {
                                    Position = new Point3D(
                                        basePosition.X + x * voxelSize,
                                        y + (random.NextDouble() - 0.5) * voxelSize,
                                        basePosition.Z + z * voxelSize
                                    ),
                                    Color = Colors.ForestGreen,
                                    Size = voxelSize,
                                    Level = level + 1
                                });
                            }
                        }
                    }
                }
            }
        }

        private void GenerateTreeMesh()
        {
            treeMesh = new MeshGeometry3D();
            originalVertices = new Point3DCollection();
            var triangleIndices = new List<int>();
            var materials = new List<Color>();

            foreach (var voxel in voxels)
            {
                AddVoxelGeometry(voxel, originalVertices, triangleIndices, materials);
            }

            treeMesh.Positions = originalVertices;
            treeMesh.TriangleIndices = new Int32Collection(triangleIndices);
        }

        private void AddVoxelGeometry(Voxel voxel, Point3DCollection vertices, 
            List<int> indices, List<Color> materials)
        {
            double s = voxel.Size / 2; // Half-size for cube vertices
            Point3D p = voxel.Position;
            int baseIndex = vertices.Count;

            // Define cube vertices
            Point3D[] cubeVertices = new[]
            {
                new Point3D(p.X - s, p.Y - s, p.Z - s), // 0
                new Point3D(p.X + s, p.Y - s, p.Z - s), // 1
                new Point3D(p.X + s, p.Y + s, p.Z - s), // 2
                new Point3D(p.X - s, p.Y + s, p.Z - s), // 3
                new Point3D(p.X - s, p.Y - s, p.Z + s), // 4
                new Point3D(p.X + s, p.Y - s, p.Z + s), // 5
                new Point3D(p.X + s, p.Y + s, p.Z + s), // 6
                new Point3D(p.X - s, p.Y + s, p.Z + s)  // 7
            };

            // Add vertices
            foreach (var vertex in cubeVertices)
            {
                vertices.Add(vertex);
                materials.Add(voxel.Color);
            }

            // Define cube faces (two triangles per face)
            int[][] faceIndices = new[]
            {
                new[] { 0, 1, 2, 0, 2, 3 }, // Front
                new[] { 1, 5, 6, 1, 6, 2 }, // Right
                new[] { 5, 4, 7, 5, 7, 6 }, // Back
                new[] { 4, 0, 3, 4, 3, 7 }, // Left
                new[] { 3, 2, 6, 3, 6, 7 }, // Top
                new[] { 4, 5, 1, 4, 1, 0 }  // Bottom
            };

            // Add indices
            foreach (var face in faceIndices)
            {
                foreach (var index in face)
                {
                    indices.Add(baseIndex + index);
                }
            }
        }

        public ModelVisual3D CreateModel()
        {
            var model = new ModelVisual3D();
            var geometry = new GeometryModel3D();
            
            geometry.Geometry = treeMesh;
            geometry.Material = new DiffuseMaterial(new SolidColorBrush(Colors.ForestGreen));
            geometry.BackMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.ForestGreen));

            model.Content = geometry;
            
            if (treeMesh.Positions != null)
            {
                originalPositions[geometry] = new Point3DCollection(treeMesh.Positions);
            }
            
            return model;
        }

        public void UpdateVoxels(double time, Vector3D windForce)
        {
            var positions = treeMesh.Positions;
            for (int i = 0; i < positions.Count; i += 8) // 8 vertices per voxel
            {
                int voxelIndex = i / 8;
                var voxel = voxels[voxelIndex];
                
                // Only move leaf voxels
                if (voxel.Color == Colors.ForestGreen)
                {
                    double windEffect = Math.Sin(time + voxelIndex) * windForce.Length;
                    Vector3D offset = windForce * windEffect * 0.1 * voxel.Level;

                    // Update all vertices of this voxel
                    for (int j = 0; j < 8; j++)
                    {
                        var originalVertex = originalVertices[i + j];
                        positions[i + j] = originalVertex + offset;
                    }
                }
            }
        }
    }
} 