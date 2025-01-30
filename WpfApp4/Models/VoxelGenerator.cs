using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WpfApp4.Models
{
    public class VoxelGenerator
    {
        public class ModelVoxel
        {
            public Rect3D Bounds { get; set; }
            public List<Model3D> ContainedModels { get; } = new();
            public int Level { get; set; } // For wind effect

            public ModelVoxel(Rect3D bounds, int level)
            {
                Bounds = bounds;
                Level = level;
            }
        }

        public static List<ModelVoxel> GenerateVoxels(Model3DGroup model, int totalVoxels)
        {
            var bounds = model.Bounds;
            
            // Calculate number of voxels per dimension to get approximately totalVoxels
            double volumeRoot = Math.Pow(totalVoxels, 1.0/3.0);
            int voxelsX = (int)Math.Ceiling(volumeRoot * bounds.SizeX / Math.Min(bounds.SizeX, Math.Min(bounds.SizeY, bounds.SizeZ)));
            int voxelsY = (int)Math.Ceiling(volumeRoot * bounds.SizeY / Math.Min(bounds.SizeX, Math.Min(bounds.SizeY, bounds.SizeZ)));
            int voxelsZ = (int)Math.Ceiling(volumeRoot * bounds.SizeZ / Math.Min(bounds.SizeX, Math.Min(bounds.SizeY, bounds.SizeZ)));

            double voxelSizeX = bounds.SizeX / voxelsX;
            double voxelSizeY = bounds.SizeY / voxelsY;
            double voxelSizeZ = bounds.SizeZ / voxelsZ;

            var voxels = new List<ModelVoxel>();

            // Create voxel grid
            for (int x = 0; x < voxelsX; x++)
            {
                for (int y = 0; y < voxelsY; y++)
                {
                    for (int z = 0; z < voxelsZ; z++)
                    {
                        var voxelBounds = new Rect3D(
                            bounds.X + x * voxelSizeX,
                            bounds.Y + y * voxelSizeY,
                            bounds.Z + z * voxelSizeZ,
                            voxelSizeX,
                            voxelSizeY,
                            voxelSizeZ
                        );

                        // Level increases with height for wind effect
                        int level = (int)(y / (double)voxelsY * 5) + 1;
                        var voxel = new ModelVoxel(voxelBounds, level);
                        voxels.Add(voxel);
                    }
                }
            }

            // Assign models to voxels
            foreach (Model3D child in model.Children)
            {
                var childBounds = child.Bounds;
                
                // Find all voxels that intersect with this model
                foreach (var voxel in voxels)
                {
                    if (BoundsIntersect(childBounds, voxel.Bounds))
                    {
                        voxel.ContainedModels.Add(child);
                    }
                }
            }

            // Remove empty voxels
            voxels.RemoveAll(v => v.ContainedModels.Count == 0);

            return voxels;
        }

        private static bool BoundsIntersect(Rect3D a, Rect3D b)
        {
            return (a.X < b.X + b.SizeX && a.X + a.SizeX > b.X &&
                    a.Y < b.Y + b.SizeY && a.Y + a.SizeY > b.Y &&
                    a.Z < b.Z + b.SizeZ && a.Z + a.SizeZ > b.Z);
        }

        public static Model3DGroup CreateVoxelizedModel(List<ModelVoxel> voxels)
        {
            var result = new Model3DGroup();

            foreach (var voxel in voxels)
            {
                var voxelGroup = new Model3DGroup();
                foreach (var model in voxel.ContainedModels)
                {
                    voxelGroup.Children.Add(model);
                }
                result.Children.Add(voxelGroup);
            }

            return result;
        }

        public static Model3DGroup CreateVoxelVisualization(List<ModelVoxel> voxels)
        {
            var result = new Model3DGroup();
            
            foreach (var voxel in voxels)
            {
                var wireframe = CreateWireframeBox(voxel.Bounds, voxel.Level);
                result.Children.Add(wireframe);
            }

            return result;
        }

        private static GeometryModel3D CreateWireframeBox(Rect3D bounds, int level)
        {
            var points = new Point3DCollection();
            var indices = new Int32Collection();

            // Calculate corners
            var corners = new Point3D[]
            {
                new(bounds.X, bounds.Y, bounds.Z),                                     // 0: front bottom left
                new(bounds.X + bounds.SizeX, bounds.Y, bounds.Z),                     // 1: front bottom right
                new(bounds.X + bounds.SizeX, bounds.Y + bounds.SizeY, bounds.Z),      // 2: front top right
                new(bounds.X, bounds.Y + bounds.SizeY, bounds.Z),                     // 3: front top left
                new(bounds.X, bounds.Y, bounds.Z + bounds.SizeZ),                     // 4: back bottom left
                new(bounds.X + bounds.SizeX, bounds.Y, bounds.Z + bounds.SizeZ),      // 5: back bottom right
                new(bounds.X + bounds.SizeX, bounds.Y + bounds.SizeY, bounds.Z + bounds.SizeZ), // 6: back top right
                new(bounds.X, bounds.Y + bounds.SizeY, bounds.Z + bounds.SizeZ)       // 7: back top left
            };

            // Add all corners
            foreach (var corner in corners)
            {
                points.Add(corner);
            }

            // Define edges (12 edges = 24 indices)
            var edgeIndices = new[]
            {
                // Front face
                0, 1, 1, 2, 2, 3, 3, 0,
                // Back face
                4, 5, 5, 6, 6, 7, 7, 4,
                // Connecting edges
                0, 4, 1, 5, 2, 6, 3, 7
            };

            foreach (var index in edgeIndices)
            {
                indices.Add(index);
            }

            var mesh = new MeshGeometry3D
            {
                Positions = points,
                TriangleIndices = indices
            };

            // Color based on level
            Color voxelColor = Color.FromRgb(
                (byte)(50 + level * 40), 
                (byte)(100 + level * 30), 
                (byte)(150 + level * 20)
            );

            var material = new DiffuseMaterial(new SolidColorBrush(voxelColor))
            {
                // Make it semi-transparent
                Brush = { Opacity = 0.3 }
            };

            return new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            };
        }
    }
} 