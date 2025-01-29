using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace WpfApp4.Models
{
    public class ModelLoader
    {

        public static Model3DGroup? LoadModel(string pathName)
        {
            try
            {
                if (!File.Exists(pathName))
                {
                    MessageBox.Show($"Model file not found: {pathName}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                var reader = new ObjReader();
                var models = reader.Read(pathName);
                if (models == null)
                {
                    MessageBox.Show("No models found in file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                return models;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading model: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }
} 