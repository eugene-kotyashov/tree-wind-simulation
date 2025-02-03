using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
// using HelixToolkit.Wpf.SharpDX.Render;
// using SharpDX.Direct2D1;

namespace WpfApp4.Models
{
    public class VoxelGenerator
    {
        public struct MeshPointInfo(Point3D pt, int id)
        {
            public Point3D oldPoint = pt;
            public int pointIndex = id;
        }
        public class ModelVoxel
        {
            public Rect3D Bounds { get; set; }
            public List<Model3D> ContainedModels { get; } = [];
            public Dictionary<MeshGeometry3D, List<MeshPointInfo>> ContainedPointsIndices { get; } = [];
            public int Level { get; set; }
            public Point3D OriginalCenter { get; set; }
            public Point3D CurrentCenter { get; set; }
            public Vector3D Velocity { get; set; } = new Vector3D(0, 0, 0);

            public double SpringStiffness { get; set; } = 0.0;
            public double SpringDamping { get; set; } = 0.0;

            public ModelVoxel(Rect3D bounds, int level)
            {
                Bounds = bounds;
                Level = level;
                OriginalCenter = new Point3D(
                    bounds.X + bounds.SizeX / 2,
                    bounds.Y + bounds.SizeY / 2,
                    bounds.Z + bounds.SizeZ / 2
                );
                CurrentCenter = OriginalCenter;
            }

            public void TransformContainedModels() {

                Vector3D movement = CurrentCenter - OriginalCenter;
                foreach (var model in ContainedModels)
                {
                    // Create single transform for the whole voxel group
                    var transform = new TranslateTransform3D(movement);
                    model.Transform = transform;
                }
            }

            public void TransformContaindedPoints()
            {
                Vector3D movement = CurrentCenter - OriginalCenter;
                foreach (var mesh in ContainedPointsIndices.Keys)
                {   
                    var pointsIndicesToChange = ContainedPointsIndices[mesh];

                    var oldPositions = mesh.Positions;
                    foreach (var pointInfo in pointsIndicesToChange)
                    {
                        oldPositions[pointInfo.pointIndex] = pointInfo.oldPoint + movement; 
                    }
                    mesh.Positions = oldPositions;
                }
                    
            }
        }

        // Adjust physics parameters for more visible movement
        
        private const double MAX_DISPLACEMENT = 10.0;   // Increased from 0.5


        static private void PrintVoxels(List<ModelVoxel> voxels)
        {

            
            int voxelId = 0;
            foreach (var voxel in voxels)
            {
                
                foreach (var item in voxel.ContainedPointsIndices) {
                    Debug.Print($"voxId: {voxelId++} model- point indices: key {item.Key.GetHashCode()}");
                    foreach (var id in item.Value)
                    {
                        Debug.Print($"   {id.pointIndex} : {id.oldPoint}");
                    }
                }
            }
         
        }

        public static List<ModelVoxel> GenerateVoxels(Model3DGroup model, int totalVoxels)
        {
            var bounds = model.Bounds;
            var center = bounds.GetCenter();
            double scaleFactor = 1.2;
            Vector3D vscale = new Vector3D(scaleFactor, scaleFactor, scaleFactor);
            var tscale = new ScaleTransform3D(vscale, center);
            var newLocation = tscale.Transform(bounds.Location);
            bounds.Location = newLocation;
            bounds.SizeX = bounds.SizeX * scaleFactor;
            bounds.SizeY = bounds.SizeY * scaleFactor;
            bounds.SizeZ = bounds.SizeZ * scaleFactor;
            
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
                    int level = y;
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
                       
                        var voxel = new ModelVoxel(voxelBounds, level);
                        voxels.Add(voxel);
                    }
                }
            }

            HashSet<Model3D> addedModels = [];

            // Assign models to voxels
            foreach (Model3D child in model.Children)
            {
                var childBounds = child.Bounds;

                Dictionary< int, MeshGeometry3D> pointsIdsAdded = [];
                // Find all voxels that intersect with this model
                foreach (var voxel in voxels)
                {
                    if (childBounds.IntersectsWith(voxel.Bounds))
                    {
                        if (!addedModels.Contains(child))

                        {
                            voxel.ContainedModels.Add(child);
                            addedModels.Add(child);
                        }

                        // Find index of a point inside this voxel if model is a GeometryModel3D
                        if (child is GeometryModel3D geometryModel)
                        {
                            if (geometryModel.Geometry is MeshGeometry3D mesh)
                            {                                
                                
                                for (int i = 0; i < mesh.Positions.Count; i++)
                                {
                                    if (pointsIdsAdded.TryGetValue(i, out MeshGeometry3D?  m))
                                    {
                                        // this means that point was already added to some voxel
                                        // so we don't try to add it second time
                                        if (m == mesh)
                                        {
                                            continue;
                                        }
                                    }
                                    var point = mesh.Positions[i];

                                    if (voxel.Bounds.Contains(point))
                                    {
                                        if (voxel.ContainedPointsIndices.ContainsKey(mesh))
                                        {
                                            voxel.ContainedPointsIndices[mesh].Add(new MeshPointInfo(point, i));
                                        }
                                        else
                                        {
                                            voxel.ContainedPointsIndices.Add(mesh, [new MeshPointInfo(point, i)]);
                                        }
                                        pointsIdsAdded.Add( i, mesh);
                                    }
                                }
                            }
                        }

                    }
                }
            }

            voxels.RemoveAll(v => (v.ContainedModels.Count == 0) && (v.ContainedPointsIndices.Count == 0));

            // PrintVoxels(voxels);
            

            return voxels;
        }


        public static Model3DGroup CreateVoxelizedModel(List<ModelVoxel> voxels)
        {
            var result = new Model3DGroup();

            foreach (var voxel in voxels)
            {
                foreach (var model in voxel.ContainedModels)
                {
                    
                    if (model is GeometryModel3D geometryModel)
                    {
                       result.Children.Add(geometryModel);
                    }
                }
            }

            return result;
        }

        public static Model3DGroup CreateVoxelVisualization(List<ModelVoxel> voxels, Color color)
        {
            var result = new Model3DGroup();
            
            foreach (var voxel in voxels)
            {
                var wireframe = CreateWireframeBox(voxel.Bounds, color);
                result.Children.Add(wireframe);
            }

            return result;
        }

        private static GeometryModel3D CreateWireframeBox(Rect3D bounds, Color voxelColor)
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

            // Create thin boxes for each edge instead of lines
            double thickness = bounds.SizeX * 0.02; // 2% of voxel size for edge thickness

            // Function to create a thin box between two points
            void AddEdge(Point3D start, Point3D end)
            {
                Vector3D direction = end - start;
                Vector3D up;
                Vector3D right;

                // Handle vertical edges specially
                if (Math.Abs(direction.Y) > Math.Abs(direction.X) && Math.Abs(direction.Y) > Math.Abs(direction.Z))
                {
                    right = new Vector3D(1, 0, 0);  // Use world X axis for vertical edges
                    up = new Vector3D(0, 0, 1);     // Use world Z axis for vertical edges
                }
                else
                {
                    up = new Vector3D(0, 1, 0);
                    right = Vector3D.CrossProduct(direction, up);
                    if (right.Length < 0.001)  // Handle near-vertical edges
                    {
                        right = new Vector3D(1, 0, 0);
                    }
                    right.Normalize();
                    up = Vector3D.CrossProduct(right, direction);
                    up.Normalize();
                }

                right *= thickness / 2;
                up *= thickness / 2;

                int baseIndex = points.Count;
                
                // Add 8 points forming a thin box
                points.Add(start - right - up);
                points.Add(start + right - up);
                points.Add(start + right + up);
                points.Add(start - right + up);
                points.Add(end - right - up);
                points.Add(end + right - up);
                points.Add(end + right + up);
                points.Add(end - right + up);

                // Add triangles for the box
                int[] boxIndices = {
                    0,1,2, 0,2,3, // front
                    4,5,6, 4,6,7, // back
                    0,4,7, 0,7,3, // left
                    1,5,6, 1,6,2, // right
                    3,2,6, 3,6,7, // top
                    0,1,5, 0,5,4  // bottom
                };

                foreach (int i in boxIndices)
                {
                    indices.Add(baseIndex + i);
                }
            }

            // Add edges
            AddEdge(corners[0], corners[1]); // Front bottom
            AddEdge(corners[1], corners[2]); // Front right
            AddEdge(corners[2], corners[3]); // Front top
            AddEdge(corners[3], corners[0]); // Front left
            AddEdge(corners[4], corners[5]); // Back bottom
            AddEdge(corners[5], corners[6]); // Back right
            AddEdge(corners[6], corners[7]); // Back top
            AddEdge(corners[7], corners[4]); // Back left
            AddEdge(corners[0], corners[4]); // Bottom left
            AddEdge(corners[1], corners[5]); // Bottom right
            AddEdge(corners[2], corners[6]); // Top right
            AddEdge(corners[3], corners[7]); // Top left

            var mesh = new MeshGeometry3D
            {
                Positions = points,
                TriangleIndices = indices
            };


            var material = new DiffuseMaterial(new SolidColorBrush(voxelColor));
           
            return new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            };
        }

        public static void UpdateVoxelPhyscis(
             Vector3D voxelCentroid,
             List<ModelVoxel> voxels,
            Vector3D windForce,
            double deltaTime)
        {

        }

        // Add method to update voxel physics
        public static void UpdateVoxelPhysics(
            List<ModelVoxel> voxels,
            Vector3D windForce,
            double deltaTime,
            bool isLowerLevelFixed)
        {
            foreach (var voxel in voxels)
            {
                // Skip voxels at the bottom (level 1)
                // if( (voxel.Level < 1) && isLowerLevelFixed) continue;
                if (isLowerLevelFixed && (voxel.Level == 0)) continue;
                

                // Calculate spring force (pulls back to original position)
                Vector3D displacement = voxel.CurrentCenter - voxel.OriginalCenter;
                Vector3D springForce = -displacement * voxel.SpringStiffness;

                // Increase wind effect

                Vector3D effectiveWind = windForce;
                
                // Add some turbulence
                /*
                double turbulence = Math.Sin(DateTime.Now.Ticks * 0.0000001 + voxel.Level) * 0.2;
                effectiveWind += new Vector3D(
                    turbulence * Math.Sin(voxel.Level),
                    turbulence * Math.Cos(voxel.Level),
                    turbulence * Math.Sin(voxel.Level * 0.5)
                );
                */

                // Apply forces
                voxel.Velocity += (springForce + effectiveWind) * deltaTime;
                voxel.Velocity *= voxel.SpringDamping;

                // Update position
                Point3D newCenter = voxel.CurrentCenter + voxel.Velocity * deltaTime;

                // Limit displacement
                Vector3D totalDisplacement = newCenter - voxel.OriginalCenter;
                if (totalDisplacement.Length > MAX_DISPLACEMENT)
                {
                    totalDisplacement.Normalize();
                    totalDisplacement *= MAX_DISPLACEMENT;
                    newCenter = voxel.OriginalCenter + totalDisplacement;
                    voxel.Velocity *= 0.5; // Reduce velocity when hitting limit
                }

                // Update voxel position
                Vector3D movement = newCenter - voxel.CurrentCenter;
                voxel.CurrentCenter = newCenter;

                // Update wireframe position
                voxel.Bounds = new Rect3D(
                    voxel.Bounds.X + movement.X,
                    voxel.Bounds.Y + movement.Y,
                    voxel.Bounds.Z + movement.Z,
                    voxel.Bounds.SizeX,
                    voxel.Bounds.SizeY,
                    voxel.Bounds.SizeZ
                );
            }
        }

        public static void UpdateVisualizationsPerModel(
            Model3DGroup modelGroup,
            Model3DGroup wireframeGroup,
            List<ModelVoxel> voxels)
        {
            int wireframeIndex = 0;
            foreach (var voxel in voxels)
            {
                voxel.TransformContainedModels();
                if (wireframeIndex < wireframeGroup.Children.Count)
                {
                    var wireframe = (GeometryModel3D)wireframeGroup.Children[wireframeIndex];
                    Vector3D movement = voxel.CurrentCenter - voxel.OriginalCenter;
                    wireframe.Transform = new TranslateTransform3D(movement);
                }
                wireframeIndex++;
            }

        }

        public static void UpdateVisualizationsPerPoint(
            Model3DGroup modelGroup,
            Model3DGroup wireframeGroup,
            List<ModelVoxel> voxels)
        {
            int wireframeIndex = 0;
            foreach (var voxel in voxels)
            {
                voxel.TransformContaindedPoints();
                if (wireframeIndex < wireframeGroup.Children.Count)
                {
                    var wireframe = (GeometryModel3D)wireframeGroup.Children[wireframeIndex];
                    Vector3D movement = voxel.CurrentCenter - voxel.OriginalCenter;
                    wireframe.Transform = new TranslateTransform3D(movement);
                }
                wireframeIndex++;

            }

        }
        
    }
} 