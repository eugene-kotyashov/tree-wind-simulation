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

internal class Vertex3
{
    public double x, y, z;

    public Vertex3(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public double X
    {
        get => x;
        set => x = value;
    }

    public double Y
    {
        get => y;
        set => y = value;
    }

    public double Z
    {
        get => z;
        set => z = value;
    }
}

internal class Vertex2
{
    public double x, y;

    public Vertex2(double x, double y)
    {
        this.x = x;
        this.y = y;
    }

    public double X
    {
        get => x;
        set => x = value;
    }

    public double Y
    {
        get => y;
        set => y = value;
    }
}

internal class Surface
{
    public List<uint> texCoords;


    public List<uint> vertices, normals;

    public Surface()
    {
        vertices = [];
        normals = [];
        texCoords = [];
    }

    public Surface(List<uint> vertices, List<uint> normals, List<uint> texCoords)
    {
        this.vertices = vertices;
        this.normals = normals;
        this.texCoords = texCoords;
    }


    public List<uint> TexCoords
    {
        get => texCoords;
        set => texCoords = value ?? throw new ArgumentNullException(nameof(value));
    }

    public List<uint> Normals
    {
        get => normals;
        set => normals = value ?? throw new ArgumentNullException(nameof(value));
    }

    public List<uint> Vertices
    {
        get => vertices;
        set => vertices = value ?? throw new ArgumentNullException(nameof(value));
    }
}

internal class Object3D
{
    public MyMaterial material { get; set; } = null!;
    public string materialName { get; set; } = string.Empty;
    public List<Vertex3> normals { get; set; }
    public List<Surface> surfaces { get; set; }
    public List<Vertex2> texCoords { get; set; }
    public List<Vertex3> vertices { get; set; }

    public Object3D()
    {
        vertices = [];
        normals = [];
        texCoords = [];
        surfaces = [];
        material = new MyMaterial();
        materialName = string.Empty;
    }

    public Object3D(List<Surface> surfaces, List<Vertex3> vertices, List<Vertex2> texCoords, List<Vertex3> normals)
    {
        this.surfaces = surfaces;
        this.vertices = vertices;
        this.texCoords = texCoords;
        this.normals = normals;
        material = new MyMaterial();
        materialName = string.Empty;
    }
}

internal class MyMaterial
{
    // Default constructor with default values
    public MyMaterial()
    {
        ambientColor = Colors.White;
        diffuseColor = Colors.White;
        specularColor = Colors.White;
        emissiveColor = Colors.Transparent;
        lightningModel = 0;
        material_file_name = null;
        reflectionIndex = 0;
        refractionIndex = 1;
        stupidTransparency = 1;
    }

    public Color ambientColor { get; set; }
    public Color diffuseColor { get; set; }
    public Color specularColor { get; set; }
    public Color emissiveColor { get; set; }
    public uint lightningModel { get; set; }
    public string? material_file_name { get; set; }
    public double reflectionIndex { get; set; }
    public double refractionIndex { get; set; }
    public double stupidTransparency { get; set; }
}

public partial class MainWindow : Window
{
    private Wind wind;
    private Dictionary<GeometryModel3D, Point3DCollection> originalPositions;
    private DispatcherTimer animationTimer;
    private double time = 0;
    private RiggedTree riggedTree;

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
        LoadModel();
    }

    private Vector3DCollection NormalsConvert(List<Vertex3> List)
    {
        var Collection = new Vector3DCollection();
        foreach (var vertex in List)
            Collection.Add(new Vector3D(vertex.X, vertex.Y,
                vertex.Z));

        return Collection;
    }

    private PointCollection TextureConvert(List<Vertex2> List)
    {
        var Collection = new PointCollection();
        foreach (var vertex in List)
            Collection.Add(new Point(vertex.X, vertex.Y));

        return Collection;
    }
    private Point3DCollection VerticesConvert(List<Vertex3> List)
    {
        var Collection = new Point3DCollection();
        foreach (var vertex in List)
            Collection.Add(new Point3D(vertex.X, vertex.Y,
                vertex.Z));

        return Collection;
    }
    public Int32Collection ConvertSurfaces(List<uint> vertexIndices)
    {
        var triangleIndices = new Int32Collection();

        for (var i = 2; i < vertexIndices.Count; i++)
        {
            triangleIndices.Add((int)vertexIndices[0]);
            triangleIndices.Add((int)vertexIndices[i - 1]);
            triangleIndices.Add((int)vertexIndices[i]);

        }

        return triangleIndices;
    }

    private ModelVisual3D AddObjectToScene(Object3D obj)
    {
        var model = new ModelVisual3D();
        var model3D = new GeometryModel3D();
        var mesh = new MeshGeometry3D();
        Console.WriteLine("Start Load GetNormals");
        foreach (var norm in obj.normals) Console.WriteLine($"X: {norm.X}, Y: {norm.Y}, Z: {norm.Z}");
        mesh.Normals = NormalsConvert(obj.normals);

        Console.WriteLine("Start Load Positions");
        mesh.Positions = VerticesConvert(obj.vertices);
        Console.WriteLine("Start Load TriangleIndices");
        var triangleIndices = new Int32Collection();

        foreach (var surf in obj.surfaces)
        {
            var surfaceIndices = ConvertSurfaces(surf.vertices);
            foreach (var index in surfaceIndices) triangleIndices.Add(index);
        }

        mesh.TriangleIndices = triangleIndices;
        mesh.TextureCoordinates = TextureConvert(obj.texCoords);

        model3D.Geometry = mesh;

        var myMaterialGroup = new MaterialGroup();

        Material mt1 = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(0, obj.material.diffuseColor.R, obj.material.diffuseColor.G, obj.material.diffuseColor.B)));
        Material mt2 = new EmissiveMaterial(new SolidColorBrush(obj.material.emissiveColor));
        Material mt3 =
            new SpecularMaterial(new SolidColorBrush(obj.material.specularColor.ChangeAlpha(0)), obj.material.reflectionIndex);
        Material texture = null;
        if (obj.material.material_file_name != null)
        {
            var imageBrush = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri(obj.material.material_file_name, UriKind.RelativeOrAbsolute)),
                Opacity = 1
            };
            
            texture = new DiffuseMaterial(imageBrush);
        }
        
        myMaterialGroup.Children.Add(mt1);
        myMaterialGroup.Children.Add(mt3);
        myMaterialGroup.Children.Add(mt2);

        if(texture!=null)
            myMaterialGroup.Children.Add(texture);
        
        model3D.Material = myMaterialGroup;

        model.Content = model3D;
        return model;
    }

    private void LoadModel()
    {
        // Make sure this path is correct relative to your executable
        string modelPath = "sakurauncut.obj";
        
        if (!File.Exists(modelPath))
        {
            MessageBox.Show($"Model file not found: {modelPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        List<Object3D> objects = [];
        var obj = new Object3D();
        string? obj_name = null;
        var surface = new Surface();
        var materialLib = "";
        var lineNumber = 0;
        int totalVertices = 0, totalTexCoords = 0, totalNormals = 0;
        string? line;
        Dictionary<string, List<Object3D>> materialsForObjects = new();
        using (var reader = new StreamReader(modelPath))
        {
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                ++lineNumber;
                if (string.IsNullOrEmpty(line))
                    continue;
                // Получить имя файла матриалов
                if (line.StartsWith("mtllib")) materialLib = line.Split(' ')[1];
                // Получить имя конкретного материала объекта
                if (line.StartsWith("usemtl")) obj.materialName = line.Split(' ')[1];
                if (line[0] == '#')
                {
                    //Проверить что все верно прочитано
                    if (line.Substring(0, 11) == "# Vertices:")
                    {
                        var parsed = line.Split(' ');
                        if (parsed.Length != 10)
                        {
                            Console.WriteLine(lineNumber);
                            Console.WriteLine(line);
                            throw new Exception();
                        }

                        if (obj.vertices.Count != uint.Parse(parsed[2].Substring(0, parsed[2].Length - 1)) ||
                            obj.normals.Count != uint.Parse(parsed[4].Substring(0, parsed[4].Length - 1)) ||
                            obj.texCoords.Count != uint.Parse(parsed[7].Substring(0, parsed[7].Length - 1)) ||
                            obj.surfaces.Count != uint.Parse(parsed[9].Substring(0, parsed[9].Length)))
                        {
                            Console.WriteLine(lineNumber);
                            Console.WriteLine(line);
                            Console.WriteLine(
                                $"Got vertices: {obj.vertices.Count}, normals: {obj.normals.Count}, texCoords: {obj.texCoords.Count}, surfaces: {obj.surfaces.Count}");
                            throw new Exception();
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                // Начинается новый объект
                if (line[0] == 'o')
                {
                    if (obj_name != null)
                    {
                        totalVertices += obj.vertices.Count;
                        totalNormals += obj.normals.Count;
                        totalTexCoords += obj.texCoords.Count;
                        objects.Add(obj);
                        if (materialsForObjects.ContainsKey(obj.materialName))
                            materialsForObjects[obj.materialName].Add(obj);
                        else
                            materialsForObjects.Add(obj.materialName, [obj]);
                    }

                    obj_name = line.Split(' ')[1];
                    obj = new Object3D();
                }
                // Чтение вершины или нормали или текстурных координат
                else if (line[0] == 'v')
                {
                    var parsed = line.Split(' ');
                    if (parsed.Length > 4 || parsed.Length < 3 ||
                        (parsed[0] != "vn" && parsed[0] != "vt" && parsed[0] != "v"))
                    {
                        Console.WriteLine(lineNumber);
                        Console.WriteLine(line);
                        throw new Exception();
                    }

                    // Прочитали текстурные координаты
                    if (parsed[0] == "vt")
                        obj.texCoords.Add(new Vertex2(
                            double.Parse(parsed[1], NumberStyles.Any, CultureInfo.InvariantCulture),
                            double.Parse(parsed[2], NumberStyles.Any, CultureInfo.InvariantCulture)));
                    //Прочитали нормаль
                    else if (parsed[0] == "vn")
                        obj.normals.Add(new Vertex3(
                            double.Parse(parsed[1], NumberStyles.Any, CultureInfo.InvariantCulture),
                            double.Parse(parsed[2], NumberStyles.Any, CultureInfo.InvariantCulture),
                            double.Parse(parsed[3], NumberStyles.Any, CultureInfo.InvariantCulture)));

                    // Прочитали вершину
                    else
                        obj.vertices.Add(new Vertex3(
                            double.Parse(parsed[1], NumberStyles.Any, CultureInfo.InvariantCulture),
                            double.Parse(parsed[2], NumberStyles.Any, CultureInfo.InvariantCulture),
                            double.Parse(parsed[3], NumberStyles.Any, CultureInfo.InvariantCulture)));
                }
                // Читаем поверхность
                else if (line[0] == 'f')
                {
                    var parsed = line.Split(' ');
                    if (parsed.Length != 4 && parsed[0] != "f")
                    {
                        Console.WriteLine(lineNumber);
                        Console.WriteLine(line);
                        throw new Exception();
                    }

                    surface = new Surface();
                    for (var i = 1; i < parsed.Length; ++i)
                    {
                        var values = parsed[i].Split('/');
                        if (values.Length != 3)
                        {
                            Console.WriteLine(lineNumber);
                            Console.WriteLine(line);
                            throw new Exception();
                        }

                        //Все параметры кроме вершины могут быть пропущены
                        if (values[0] != "")
                        {
                            if (uint.Parse(values[0]) - totalVertices <= 0)
                            {
                                Console.WriteLine(lineNumber);
                                Console.WriteLine(line);
                                throw new Exception();
                            }

                            surface.vertices.Add((uint)(uint.Parse(values[0]) - 1 - totalVertices));
                        }

                        if (values[1] != "")
                        {
                            if (uint.Parse(values[1]) - totalTexCoords <= 0)
                            {
                                Console.WriteLine(lineNumber);
                                Console.WriteLine(line);
                                throw new Exception();
                            }

                            surface.texCoords.Add((uint)(uint.Parse(values[1]) - 1 - totalTexCoords));
                        }

                        if (values[2] != "")
                        {
                            if (uint.Parse(values[2]) - totalNormals <= 0)
                            {
                                Console.WriteLine(lineNumber);
                                Console.WriteLine(line);
                                throw new Exception();
                            }

                            surface.normals.Add((uint)(uint.Parse(values[2]) - 1 - totalNormals));
                        }
                    }

                    obj.surfaces.Add(surface);
                }
            }
        }

        // Добавить последний объект
        objects.Add(obj);

        if (materialsForObjects.ContainsKey(obj.materialName))
            materialsForObjects[obj.materialName].Add(obj);
        else
            materialsForObjects.Add(obj.materialName, [obj]);
        Console.WriteLine(materialLib);
        
        // Читаем mtl
        lineNumber = 0;
        var currentMaterialName = "";
        var currentMaterial = new MyMaterial();
        using (var reader = new StreamReader($"{materialLib}"))
        {
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                ++lineNumber;
                if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                    continue;
                if (line.StartsWith("newmtl "))
                {
                    if (!string.IsNullOrEmpty(currentMaterialName))
                        foreach (var elem in materialsForObjects[currentMaterialName])
                            elem.material = currentMaterial;
                    currentMaterialName = line.Split(' ')[1];
                    currentMaterial = new MyMaterial();
                }
                else if (line.StartsWith("Ns "))
                {
                    currentMaterial.reflectionIndex =
                        double.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);
                }
                else if (line.StartsWith("Ka "))
                {
                    var values = line.Split(' ');
                    currentMaterial.ambientColor = Color.FromArgb(
                        255,
                        (byte)Math.Round(255 * double.Parse(values[1], CultureInfo.InvariantCulture)),
                        (byte)Math.Round(255 * double.Parse(values[2], CultureInfo.InvariantCulture)),
                        (byte)Math.Round(255 * double.Parse(values[3], CultureInfo.InvariantCulture))
                    );
                }
                else if (line.StartsWith("Kd "))
                {
                    var values = line.Split(' ');
                    currentMaterial.diffuseColor = Color.FromArgb(
                        255,
                        (byte)Math.Round(255 * double.Parse(values[1], CultureInfo.InvariantCulture)),
                        (byte)Math.Round(255 * double.Parse(values[2], CultureInfo.InvariantCulture)),
                        (byte)Math.Round(255 * double.Parse(values[3], CultureInfo.InvariantCulture)));
                }
                else if (line.StartsWith("Ks "))
                {
                    var values = line.Split(' ');
                    currentMaterial.specularColor = Color.FromArgb(
                        255,
                        (byte)Math.Round(255 * double.Parse(values[1], CultureInfo.InvariantCulture)),
                        (byte)Math.Round(255 * double.Parse(values[2], CultureInfo.InvariantCulture)),
                        (byte)Math.Round(255 * double.Parse(values[3], CultureInfo.InvariantCulture)));
                }
                else if (line.StartsWith("Ke "))
                {
                    var values = line.Split(' ');
                    currentMaterial.emissiveColor = Color.FromArgb(
                        255,
                        (byte)Math.Round(255 * double.Parse(values[1], CultureInfo.InvariantCulture)),
                        (byte)Math.Round(255 * double.Parse(values[2], CultureInfo.InvariantCulture)),
                        (byte)Math.Round(255 * double.Parse(values[3], CultureInfo.InvariantCulture)));
                }
                else if (line.StartsWith("Ni "))
                {
                    currentMaterial.refractionIndex =
                        double.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);
                }
                else if (line.StartsWith("d "))
                {
                    currentMaterial.stupidTransparency =
                        double.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);
                }
                else if (line.StartsWith("illum "))
                {
                    currentMaterial.lightningModel = uint.Parse(line.Split(' ')[1]);
                }
                else if (line.StartsWith("map_Kd "))
                {
                    currentMaterial.material_file_name = line.Split(' ')[1];
                }
            }
        }

        var viewport = FindName("helixViewport") as HelixViewport3D;

        // Добавить все объекты на сцену
        foreach (var myobj in objects) Viewport3D.Children.Add(AddObjectToScene(myobj));

        // After loading, store original positions
        foreach (var model in Viewport3D.Children.OfType<ModelVisual3D>())
        {
            if (model.Content is GeometryModel3D geometryModel)
            {
                var mesh = (MeshGeometry3D)geometryModel.Geometry;
                originalPositions[geometryModel] = new Point3DCollection(mesh.Positions);
            }
        }
        
        // Start animation
        animationTimer.Start();

        // Add rigged tree
        riggedTree = new RiggedTree(
            new Point3D(-2, 0, 0),  // Position to the left of OBJ model
            4.0,                    // Taller height
            0.5,                    // Thicker radius
            originalPositions       // Pass the dictionary
        );
        Viewport3D.Children.Add(riggedTree.CreateModel());

        // Add a light to better see the models
        var directionalLight = new DirectionalLight(Colors.White, new Vector3D(-1, -1, -1));
        Viewport3D.Children.Add(new ModelVisual3D { Content = directionalLight });
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        time += 0.016; // Time increment
        UpdateWindPhysics();
    }

    private void UpdateWindPhysics()
    {
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
                    
                    // Get wind force for this vertex
                    var windForce = wind.GetForce(pos, time);
                    
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
            var windForce = wind.GetForce(new Point3D(0, 0, 0), time);
            riggedTree.UpdateBones(time, windForce);
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


