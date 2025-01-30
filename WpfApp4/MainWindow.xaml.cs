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
using System.Diagnostics;

// using Color = System.Drawing.Color;

namespace WpfApp4;


public partial class MainWindow : Window
{
    private Wind wind;
    private Dictionary<GeometryModel3D, Point3DCollection> originalPositions;
    private DispatcherTimer animationTimer;
    private double time = 0;
    private double deltaT = 0.5;
    private RiggedTree riggedTree;
    private VoxelTree voxelTree;
    private List<VoxelGenerator.ModelVoxel> voxels;
    private Model3DGroup voxelVisualization;
    private Model3DGroup voxelizedModel;
    private ModelVisual3D voxelizedVisual;
    private ModelVisual3D wireframeVisual;
    private Model3DGroup windArrow;
    private Point3D arrowPosition;

    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize collections
        originalPositions = new Dictionary<GeometryModel3D, Point3DCollection>();
        
        // Initialize wind (blowing in X direction)
        wind = new Wind(new Vector3D(1, 0, 0), 0.5);
        
        // Setup animation timer
        animationTimer = new DispatcherTimer();
        animationTimer.Interval = TimeSpan.FromMilliseconds(1000*deltaT);
        animationTimer.Tick += AnimationTimer_Tick;

        // Set up viewport interaction
        Viewport3D.RotateGesture = new MouseGesture(MouseAction.LeftClick);
        Viewport3D.PanGesture = new MouseGesture(MouseAction.RightClick);
        
        // Load the model
        // var objects = ModelLoader.LoadObjModel("tree_flowers.obj");
        // Добавить все объекты на сцену
        // foreach (var myobj in objects) Viewport3D.Children.Add(Object3D.AddObjectToScene(myobj));
      
        
        var pot = ModelLoader.LoadModel("tree_pot.obj")  ?? new Model3DGroup();
        var flowers = ModelLoader.LoadModel("tree_flowers.obj") ?? new Model3DGroup();
        Debug.WriteLine(flowers.Children.Count);
        var branches = ModelLoader.LoadModel("tree_branches.obj") ?? new Model3DGroup();
        var wholeTree = new Model3DGroup();
        wholeTree.Children.Add(branches);
        wholeTree.Children.Add(branches);
        wholeTree.Children.Add(flowers);

        // Generate voxels (e.g., 64 voxels total)
        voxels = VoxelGenerator.GenerateVoxels(flowers, 64);
        Debug.WriteLine($"Generated {voxels.Count} non-empty voxels");

        // Create and add voxelized model
        voxelizedModel = VoxelGenerator.CreateVoxelizedModel(voxels);
        voxelizedVisual = new ModelVisual3D { Content = voxelizedModel };
        Viewport3D.Children.Add(voxelizedVisual);

        // Create and add voxel visualization (wireframe)
        voxelVisualization = VoxelGenerator.CreateVoxelVisualization(voxels);
        wireframeVisual = new ModelVisual3D { Content = voxelVisualization };
        Viewport3D.Children.Add(wireframeVisual);

        // Add lighting to better see the models
        var directionalLight = new DirectionalLight(Colors.White, new Vector3D(-1, -1, -1));
        Viewport3D.Children.Add(new ModelVisual3D { Content = directionalLight });

        // After loading the OBJ model, get its height
        double modelHeight = wholeTree.Bounds.SizeY;
        double modelX = 0;
        

        // Add rigged tree with matching height
        /*
        riggedTree = new RiggedTree(
            new Point3D(modelX - 2, 0, 0),  // Position relative to model
            modelHeight,                     // Use model's height
            modelHeight * 0.1,              // Radius proportional to height
            originalPositions
        );
        Viewport3D.Children.Add(riggedTree.CreateModel());
        
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

        */
        // Create wind direction arrow
        arrowPosition = new Point3D(modelX - 2, modelHeight * 1.2, 0);
        windArrow = WindArrow.CreateArrow(
            new Point3D(0, 0, 0),  // Will be positioned by transform
            new Vector3D(1, 0, 0),  // Initial direction
            modelHeight * 0.3,      // Arrow size
            Colors.Red              // Arrow color
        );
        Viewport3D.Children.Add(new ModelVisual3D { Content = windArrow });

        // Start animation
        animationTimer.Start();
    }

    

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        time += deltaT; // Time increment
        UpdateWindPhysics();
    }

    private void UpdateWindPhysics()
    {
        var windForce = wind.GetForce(time);

        // Update wind arrow
        WindArrow.UpdateArrow(windArrow, windForce, arrowPosition);

        if (voxels != null)
        {
            VoxelGenerator.UpdateVoxelPhysics(voxels, windForce, deltaT);
            
            // Update existing geometry instead of creating new
            VoxelGenerator.UpdateVisualizations(voxelizedModel, voxelVisualization, voxels);
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


