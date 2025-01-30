using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using System.Collections.Generic;
using System.Windows.Threading;
using WpfApp4.Physics;  // Add this at the top
using WpfApp4.Models;  // Add this for RiggedTree

// using Color = System.Drawing.Color;

namespace WpfApp4;


public partial class MainWindow : Window
{
    private Wind wind;
    private Dictionary<GeometryModel3D, Point3DCollection> originalPositions;
    private DispatcherTimer animationTimer;
    private double time = 0;
    private RiggedTree riggedTree;
    private VoxelTree voxelTree;

    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize collections
        originalPositions = new Dictionary<GeometryModel3D, Point3DCollection>();
        
        // Initialize wind (blowing in X direction)
        wind = new Wind(new Vector3D(1, 0, 0), 0.5);
        
        // Setup animation timer
        animationTimer = new DispatcherTimer();
        animationTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
        animationTimer.Tick += AnimationTimer_Tick;

        // Set up viewport interaction
        Viewport3D.RotateGesture = new MouseGesture(MouseAction.LeftClick);
        Viewport3D.PanGesture = new MouseGesture(MouseAction.RightClick);
        
        // Load the model
        var objects = ModelLoader.LoadObjModel("sakurauncut.obj");
        // Добавить все объекты на сцену
        foreach (var myobj in objects) Viewport3D.Children.Add(Object3D.AddObjectToScene(myobj));
       /*
        var modelGroup = ModelLoader.LoadModel("tree_flowers.obj");
        if (modelGroup != null)
        {
             
            ModelVisual3D visual = new ModelVisual3D { Content = modelGroup };
            Viewport3D.Children.Add(visual);
      
        }
       */
        // After loading, store original positions
        foreach (var model in Viewport3D.Children.OfType<ModelVisual3D>())
        {
            if (model.Content is GeometryModel3D geometryModel)
            {
                var mesh = (MeshGeometry3D)geometryModel.Geometry;
                originalPositions[geometryModel] = new Point3DCollection(mesh.Positions);
            }
        }
        

        // After loading the OBJ model, get its height
        double modelHeight = 10;
        double modelX = 0;
        foreach (var model in Viewport3D.Children.OfType<ModelVisual3D>())
        {
            if (model.Content is GeometryModel3D geometryModel)
            {
                var mesh = (MeshGeometry3D)geometryModel.Geometry;
                modelHeight = Math.Max(modelHeight, mesh.Bounds.SizeY);
                modelX = mesh.Bounds.X - mesh.Bounds.SizeX; // Get leftmost point
            }
        }

        // Add rigged tree with matching height
        /*
        riggedTree = new RiggedTree(
            new Point3D(modelX - 2, 0, 0),  // Position relative to model
            modelHeight,                     // Use model's height
            modelHeight * 0.1,              // Radius proportional to height
            originalPositions
        );
        Viewport3D.Children.Add(riggedTree.CreateModel());
        */
        // Add voxel tree with matching height
        voxelTree = new VoxelTree(
            new Point3D(modelX + 4, 0, 0),  // Position to the right of the OBJ model
            modelHeight,                     // Match OBJ model height
            modelHeight * 0.4,              // Width proportional to height
            originalPositions
        );
        Viewport3D.Children.Add(voxelTree.CreateModel());
    
        // Add a light to better see the models
        var directionalLight = new DirectionalLight(Colors.White, new Vector3D(-1, -1, -1));
        Viewport3D.Children.Add(new ModelVisual3D { Content = directionalLight });

        
        // Start animation
        animationTimer.Start();
    }

    

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        time += 0.016; // Time increment
        UpdateWindPhysics();
    }

    private void UpdateWindPhysics()
    {
        // Get wind force for this vertex
        var windForce = wind.GetForce(time);

        foreach (var model in Viewport3D.Children.OfType<ModelVisual3D>())
        {
            if (model.Content is GeometryModel3D geometryModel)
            {
                var mesh = (MeshGeometry3D)geometryModel.Geometry;
                var original = originalPositions[geometryModel];
                var newPositions = new Point3DCollection();

                // Get the minimum Y value from the mesh
                double minY = mesh.Bounds.Y;
                double meshHeight = mesh.Bounds.SizeY;

                for (int i = 0; i < original.Count; i++)
                {
                    var pos = original[i];
                    
                    // Calculate height factor (more movement at higher positions)
                    double heightFactor = Math.Max(0, (pos.Y - minY) / meshHeight);
                    
                    
                    // Add wave movement
                    double waveX = Math.Sin(time * 2 + pos.X * 0.5) * heightFactor * 0.1;
                    double waveY = Math.Cos(time * 2 + pos.Y * 0.5) * heightFactor * 0.1;
                    double waveZ = Math.Sin(time * 2 + pos.Z * 0.5) * heightFactor * 0.1;

                    // Apply forces
                    var newPos = new Point3D(
                        pos.X + windForce.X * heightFactor + waveX,
                        pos.Y + windForce.Y * heightFactor + waveY,
                        pos.Z + windForce.Z * heightFactor + waveZ
                    );

                    newPositions.Add(newPos);
                }

                mesh.Positions = newPositions;
            }
        }

        // Update rigged tree
        if (riggedTree != null)
        {
            
            riggedTree.UpdateBones(time, windForce);
        }

        // Update voxel tree
        if (voxelTree != null)
        {
            voxelTree.UpdateVoxels(time, windForce);
        }
    }

    // Add UI controls for wind parameters
    private void WindStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (wind != null)
            wind.Strength = e.NewValue;
    }

    private void WindDirectionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (wind != null)
        {
            double angle = e.NewValue * Math.PI / 180;
            wind.Direction = new Vector3D(Math.Cos(angle), 0, Math.Sin(angle));
        }
    }
}


