using System.Windows.Media.Media3D;
using System.Windows.Media;
using HelixToolkit.Wpf;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D.Converters;

namespace WpfApp4.Models
{
    public class RiggedTree
    {
        private class TreeBone
        {
            public Point3D Start { get; set; }
            public Point3D End { get; set; }
            public List<TreeBone> Children { get; } = new();
            public Matrix3D BindPose { get; set; }
            public int Index { get; set; }
        }

        private List<TreeBone> bones = new();
        private MeshGeometry3D treeMesh;
        private Point3DCollection originalVertices;
        private List<int> boneIndices = new();
        private Point3DCollection boneWeights;
        private readonly Dictionary<GeometryModel3D, Point3DCollection> originalPositions;

        public RiggedTree(Point3D position, double height, double radius, 
            Dictionary<GeometryModel3D, Point3DCollection> originalPositions)
        {
            this.originalPositions = originalPositions;
            GenerateTreeStructure(position, height * 2, radius * 2); // Make tree bigger
            GenerateTreeMesh();
            CalculateSkinningData();
        }

        private void GenerateTreeStructure(Point3D basePosition, double height, double radius)
        {
            // Create trunk
            var trunk = new TreeBone
            {
                Start = basePosition,
                End = new Point3D(basePosition.X, basePosition.Y + height * 0.6, basePosition.Z),
                Index = 0
            };
            bones.Add(trunk);

            // Create main branches
            Random rand = new Random(42);
            int numMainBranches = 8;
            int branchesPerLevel = 3;  // Number of branches at each level
            int numLevels = 3;         // Number of levels along trunk
            int boneIndex = 1;

            for (int level = 0; level < numLevels; level++)
            {
                // Calculate height along trunk for this level (0.3 to 0.9 of trunk height)
                double heightPercent = 0.3 + (level * 0.3);
                Point3D levelPosition = new Point3D(
                    trunk.Start.X + (trunk.End.X - trunk.Start.X) * heightPercent,
                    trunk.Start.Y + (trunk.End.Y - trunk.Start.Y) * heightPercent,
                    trunk.Start.Z + (trunk.End.Z - trunk.Start.Z) * heightPercent
                );

                // Create branches at this level
                for (int i = 0; i < branchesPerLevel; i++)
                {
                    double angle = (2 * Math.PI * i) / branchesPerLevel + (level * Math.PI / branchesPerLevel);
                    double branchHeight = height * (0.4 - level * 0.1) + rand.NextDouble() * height * 0.2;
                    double branchAngle = Math.PI / 3 + rand.NextDouble() * Math.PI / 3;

                    var mainBranch = new TreeBone
                    {
                        Start = new Point3D(
                            levelPosition.X + Math.Cos(angle) * radius * 0.3,
                            levelPosition.Y,
                            levelPosition.Z + Math.Sin(angle) * radius * 0.3
                        ),
                        End = new Point3D(
                            levelPosition.X + Math.Cos(angle) * branchHeight * Math.Sin(branchAngle),
                            levelPosition.Y + branchHeight * Math.Cos(branchAngle),
                            levelPosition.Z + Math.Sin(angle) * branchHeight * Math.Sin(branchAngle)
                        ),
                        Index = boneIndex++
                    };
                    
                    trunk.Children.Add(mainBranch);
                    bones.Add(mainBranch);

                    // Add secondary branches
                    int numSecondaryBranches = 3;
                    for (int j = 0; j < numSecondaryBranches; j++)
                    {
                        double t = 0.3 + (j * 0.25);
                        Point3D branchPoint = new Point3D(
                            mainBranch.Start.X + (mainBranch.End.X - mainBranch.Start.X) * t,
                            mainBranch.Start.Y + (mainBranch.End.Y - mainBranch.Start.Y) * t,
                            mainBranch.Start.Z + (mainBranch.End.Z - mainBranch.Start.Z) * t
                        );

                        // Adjust secondary branch size based on level
                        double levelFactor = 1.0 - (level * 0.2);
                        double secondaryAngle = angle + Math.PI * (0.25 + rand.NextDouble() * 0.5) * (j % 2 == 0 ? 1 : -1);
                        double secondaryHeight = branchHeight * 0.3 * (1.0 - t * 0.5) * levelFactor;
                        double secondaryBranchAngle = branchAngle * 0.8;

                        var secondaryBranch = new TreeBone
                        {
                            Start = branchPoint,
                            End = new Point3D(
                                branchPoint.X + Math.Cos(secondaryAngle) * secondaryHeight * Math.Sin(secondaryBranchAngle),
                                branchPoint.Y + secondaryHeight * Math.Cos(secondaryBranchAngle),
                                branchPoint.Z + Math.Sin(secondaryAngle) * secondaryHeight * Math.Sin(secondaryBranchAngle)
                            ),
                            Index = boneIndex++
                        };

                        mainBranch.Children.Add(secondaryBranch);
                        bones.Add(secondaryBranch);
                    }
                }
            }
        }

        private void GenerateTreeMesh()
        {
            treeMesh = new MeshGeometry3D();
            originalVertices = new Point3DCollection();
            var triangleIndices = new List<int>();

            // Generate cylinder segments for each bone
            foreach (var bone in bones)
            {
                AddBoneGeometry(bone, originalVertices, triangleIndices);
            }

            treeMesh.Positions = originalVertices;
            treeMesh.TriangleIndices = new Int32Collection(triangleIndices);
        }

        private void AddBoneGeometry(TreeBone bone, Point3DCollection vertices, List<int> indices)
        {
            int segments = 8;
            // Adjust radius based on bone level
            double radius;
            if (bone == bones[0]) // trunk
                radius = 0.1;
            else if (bone.Start.Y <= bones[0].End.Y + 0.1) // main branches
                radius = 0.05;
            else // secondary branches
                radius = 0.025;

            Vector3D direction = bone.End - bone.Start;
            Vector3D right = Vector3D.CrossProduct(direction, new Vector3D(0, 0, 1));
            right.Normalize();
            Vector3D forward = Vector3D.CrossProduct(right, direction);
            forward.Normalize();

            // Create vertices around the bone
            int baseIndex = vertices.Count;
            for (int i = 0; i <= segments; i++)
            {
                double angle = (2 * Math.PI * i) / segments;
                Vector3D offset = right * Math.Cos(angle) * radius + forward * Math.Sin(angle) * radius;
                
                vertices.Add(bone.Start + offset);
                vertices.Add(bone.End + offset * 0.5); // Taper at the end
            }

            // Create triangles
            for (int i = 0; i < segments; i++)
            {
                int i0 = baseIndex + i * 2;
                int i1 = baseIndex + i * 2 + 1;
                int i2 = baseIndex + ((i + 1) % segments) * 2;
                int i3 = baseIndex + ((i + 1) % segments) * 2 + 1;

                indices.Add(i0); indices.Add(i1); indices.Add(i2);
                indices.Add(i2); indices.Add(i1); indices.Add(i3);
            }
        }

        private void CalculateSkinningData()
        {
            boneIndices = new List<int>();
            boneWeights = new Point3DCollection();

            for (int i = 0; i < originalVertices.Count; i++)
            {
                var vertex = originalVertices[i];
                var closestBone = FindClosestBone(vertex);
                
                // Store bone index and weight (1.0 for now - could be enhanced with smooth skinning)
                boneIndices.Add(closestBone.Index);
                boneWeights.Add(new Point3D(1, 0, 0));
            }
        }

        private TreeBone FindClosestBone(Point3D vertex)
        {
            TreeBone closest = bones[0];
            double minDist = double.MaxValue;

            foreach (var bone in bones)
            {
                double dist = PointToLineDistance(vertex, bone.Start, bone.End);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = bone;
                }
            }

            return closest;
        }

        private double PointToLineDistance(Point3D point, Point3D lineStart, Point3D lineEnd)
        {
            Vector3D line = lineEnd - lineStart;
            Vector3D pointToStart = point - lineStart;
            double t = Vector3D.DotProduct(pointToStart, line) / line.LengthSquared;
            t = Math.Max(0, Math.Min(1, t));
            Point3D projection = lineStart + t * line;
            return (point - projection).Length;
        }

        public ModelVisual3D CreateModel()
        {
            var model = new ModelVisual3D();
            var geometry = new GeometryModel3D();
            
            geometry.Geometry = treeMesh;
            // Make tree more visible with darker color
            geometry.Material = new DiffuseMaterial(new SolidColorBrush(Colors.DarkGreen));
            geometry.BackMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.DarkGreen));

            model.Content = geometry;
            
            if (treeMesh.Positions != null)
            {
                originalPositions[geometry] = new Point3DCollection(treeMesh.Positions);
            }
            
            return model;
        }

        public void UpdateBones(double time, Vector3D windForce)
        {
            foreach (var bone in bones.Skip(1)) // Skip trunk
            {
                // Stronger effect on secondary branches
                double windMultiplier = bone.Start.Y > bones[0].End.Y + 0.1 ? 1.5 : 1.0;
                
                // Apply wind force with some randomness
                double windEffect = Math.Sin(time + bone.Index) * windForce.Length;
                Vector3D offset = windForce * windEffect * 0.1 * windMultiplier;
                
                // Update vertex positions based on bone movement
                for (int i = 0; i < originalVertices.Count; i++)
                {
                    if (boneIndices[i] == bone.Index)
                    {
                        var vertex = originalVertices[i];
                        treeMesh.Positions[i] = vertex + offset;
                    }
                }
            }
        }
    }
} 