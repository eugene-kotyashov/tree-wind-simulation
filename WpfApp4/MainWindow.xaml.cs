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
    private double deltaT = 0.05;
    private RiggedTree riggedTree;
    private VoxelTree voxelTree;
    private List<VoxelGenerator.ModelVoxel> flowerVoxels;
    private List<VoxelGenerator.ModelVoxel> branchVoxels;
    private Model3DGroup flowerVoxelVisualization;
    private Model3DGroup flowerVoxelizedModel;
    private ModelVisual3D flowerVoxelizedVisual;
    private ModelVisual3D flowerVoxelWireframeVisual;

    private Model3DGroup branchVoxelVisualization;
    private Model3DGroup branchVoxelizedModel;
    private ModelVisual3D branchVoxelizedVisual;
    private ModelVisual3D branchVoxelWireframeVisual;

    private Model3DGroup windArrow;
    private Point3D arrowPosition;

    // Update the visibility state fields
    private bool showFlowersSolid = true;
    private bool showFlowersWireframe = true;
    private bool showBranchesSolid = true;
    private bool showBranchesWireframe = true;

    private const double FLOVER_SPRING_STIFFNESS = 1.0;
    private const double BRANCH_SPRING_STIFFNESS = 6.0;
    private const double FLOVER_SPRING_DUMPING = 0.99;
    private const double BRANCH_SPRING_DUMPING = 0.95;
    private Vector3D flowerCentroid = new Vector3D();

    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize collections
        originalPositions = new Dictionary<GeometryModel3D, Point3DCollection>();
        
        // Initialize wind (blowing in X direction)
        wind = new Wind(new Vector3D(1, 0, 0), 0.5, 1);
        
        // Setup animation timer
        animationTimer = new DispatcherTimer();
        animationTimer.Interval = TimeSpan.FromMilliseconds(1000*deltaT);
        animationTimer.Tick += AnimationTimer_Tick;

        // Set up viewport interaction
        Viewport3D.RotateGesture = new MouseGesture(MouseAction.LeftClick);
        Viewport3D.PanGesture = new MouseGesture(MouseAction.RightClick);
        var pot = ModelLoader.LoadModel("tree_pot.obj")  ?? new Model3DGroup();
        var flowers = ModelLoader.LoadModel("tree_flowers_combined.obj") ?? new Model3DGroup();
        var branches = ModelLoader.LoadModel("tree_branches.obj") ?? new Model3DGroup();

        /*
        var tree = ModelLoader.LoadObjModel("sakurauncut.obj");
        if (tree == null) return;
        var pot = tree[2];
        var branches = tree[1];
        */
        Debug.WriteLine("$flowers count{flowers.Children.Count}");
        Debug.WriteLine($"branches count {branches.Children.Count}");

        var wholeTree = new Model3DGroup();
        wholeTree.Children.Add(pot);
        wholeTree.Children.Add(branches);
        wholeTree.Children.Add(flowers);


        // add pot model
        var potVisual = new ModelVisual3D { Content = pot };
        Viewport3D.Children.Add(potVisual);
        
        // Generate voxels for flowers (reduced count)
        flowerVoxels = VoxelGenerator.GenerateVoxels(flowers, 1500); // Reduced from 200
        Debug.WriteLine($"Generated {flowerVoxels.Count} non-empty voxels for flowers");
        // Calculate centroid of flower voxels
        
        foreach (var voxel in flowerVoxels)
        {
            flowerCentroid += new Vector3D(voxel.OriginalCenter.X, voxel.OriginalCenter.Y, voxel.OriginalCenter.Z);
        }
        flowerCentroid /= flowerVoxels.Count;
        foreach(var v in flowerVoxels)
        {
            v.SpringStiffness = FLOVER_SPRING_STIFFNESS * (1.0 + 1.0 * Random.Shared.NextDouble());
            v.SpringDamping = FLOVER_SPRING_DUMPING;
        }

        // Create and add voxelized model for flowers
        flowerVoxelizedModel = VoxelGenerator.CreateVoxelizedModel(flowerVoxels);
        flowerVoxelizedVisual = new ModelVisual3D { Content = flowerVoxelizedModel };

        flowerVoxelVisualization = 
            VoxelGenerator.CreateVoxelVisualization(
                flowerVoxels, Color.FromRgb(0, 1, 0));
        flowerVoxelWireframeVisual = new ModelVisual3D { Content = flowerVoxelVisualization };
        Viewport3D.Children.Add(flowerVoxelizedVisual);
        Viewport3D.Children.Add(flowerVoxelWireframeVisual);
        
        // Generate voxels for branches (reduced count)
        branchVoxels = VoxelGenerator.GenerateVoxels(branches, 100); // Reduced from 50
        Debug.WriteLine($"Generated {branchVoxels.Count} non-empty voxels for branches");

        foreach(var v in branchVoxels)
        {
            v.SpringStiffness = BRANCH_SPRING_STIFFNESS/(1.0 + v.Level);
            v.SpringDamping = BRANCH_SPRING_DUMPING;
        }

        // Create and add voxelized model for branches
        branchVoxelizedModel = VoxelGenerator.CreateVoxelizedModel(branchVoxels);
        branchVoxelizedVisual = new ModelVisual3D { Content = branchVoxelizedModel };

        branchVoxelVisualization = 
            VoxelGenerator.CreateVoxelVisualization(branchVoxels, Color.FromRgb(1, 0, 0));
        branchVoxelWireframeVisual = new ModelVisual3D {  Content=branchVoxelVisualization };

        Viewport3D.Children.Add(branchVoxelizedVisual);
        Viewport3D.Children.Add(branchVoxelWireframeVisual);


        // Add lighting to better see the models
        var directionalLight = new DirectionalLight(Colors.White, new Vector3D(-1, -1, -1));
        Viewport3D.Children.Add(new ModelVisual3D { Content = directionalLight });

        // After loading the OBJ model, get its height
        double modelHeight = wholeTree.Bounds.SizeY;
        double modelX = 0;
        
        // Create wind direction arrow
        arrowPosition = new Point3D(modelX - 2, modelHeight * 1.1, 0);
        windArrow = WindArrow.CreateArrow(
            new Point3D(0, 0, 0),  // Will be positioned by transform
            new Vector3D(1, 0, 0),  // Initial direction
            modelHeight * 0.3,      // Arrow size
            Colors.Red              // Arrow color
        );
        Viewport3D.Children.Add(new ModelVisual3D { Content = windArrow });

        // Start animation
        animationTimer.Start();

        // After adding visualizations to viewport, set initial visibility
        UpdateVisualizationVisibility();
    }

    

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        time += deltaT; // Time increment
        UpdateWindPhysics();
    }

    private void UpdateWindPhysics()
    {
        var windForce = wind.GetForce(time);

        // Update wind arrow - scale the arrow based on wind strength
        var windDirection = wind.Direction;
        windDirection.Normalize();  // Ensure unit vector
        var scaledDirection = windDirection * wind.Strength;  // Scale by wind strength
        WindArrow.UpdateArrow(windArrow, scaledDirection, arrowPosition);

        
        if (flowerVoxels != null)
        {
            VoxelGenerator.UpdateVoxelPhysics(flowerVoxels, windForce, deltaT, false);
            
            // Update existing geometry instead of creating new
            VoxelGenerator.UpdateVisualizationsPerPoint(flowerVoxelizedModel, flowerVoxelVisualization, flowerVoxels);
        }
        
        if (branchVoxels != null)
        {
            VoxelGenerator.UpdateVoxelPhysics(branchVoxels, windForce, deltaT, true);

            Viewport3D.Children.Remove(branchVoxelizedVisual);

            // Update existing geometry instead of creating new
            VoxelGenerator.UpdateVisualizationsPerPoint(branchVoxelizedModel, branchVoxelVisualization, branchVoxels);

            Viewport3D.Children.Add(branchVoxelizedVisual);
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

    // Update the visibility control method
    private void UpdateVisualizationVisibility()
    {
        if (flowerVoxelizedVisual != null)
            flowerVoxelizedVisual.Content = showFlowersSolid ? flowerVoxelizedModel : null;
        
        if (flowerVoxelWireframeVisual != null)
            flowerVoxelWireframeVisual.Content = showFlowersWireframe ? flowerVoxelVisualization : null;

        if (branchVoxelizedVisual != null)
            branchVoxelizedVisual.Content = showBranchesSolid ? branchVoxelizedModel : null;
        
        if (branchVoxelWireframeVisual != null)
            branchVoxelWireframeVisual.Content = showBranchesWireframe ? branchVoxelVisualization : null;
    }

    // Add new event handlers
    private void FlowersSolidCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        showFlowersSolid = true;
        UpdateVisualizationVisibility();
    }

    private void FlowersSolidCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        showFlowersSolid = false;
        UpdateVisualizationVisibility();
    }

    private void FlowersWireframeCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        showFlowersWireframe = true;
        UpdateVisualizationVisibility();
    }

    private void FlowersWireframeCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        showFlowersWireframe = false;
        UpdateVisualizationVisibility();
    }

    private void BranchesSolidCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        showBranchesSolid = true;
        UpdateVisualizationVisibility();
    }

    private void BranchesSolidCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        showBranchesSolid = false;
        UpdateVisualizationVisibility();
    }

    private void BranchesWireframeCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        showBranchesWireframe = true;
        UpdateVisualizationVisibility();
    }

    private void BranchesWireframeCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        showBranchesWireframe = false;
        UpdateVisualizationVisibility();
    }
}


