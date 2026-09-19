using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Ersm_Animation_App.Models;
using Ersm_Animation_App.Services;
using System.Collections.Generic;

namespace Ersm_Animation_App
{
    public class RecentProjectModel
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }

    public partial class WelcomeWindow : Window
    {
        public WelcomeWindow()
        {
            InitializeComponent();
            
            // Load recent projects from settings (for now empty until settings service is built)
            RecentProjectsList.ItemsSource = new List<RecentProjectModel>();
        }

        private void CreateProject_Click(object? sender, RoutedEventArgs e)
        {
            var project = new AnimationProject
            {
                ProjectName = ProjectNameInput.Text ?? "Untitled",
                CanvasWidth = (int)(WidthInput.Value ?? 1920),
                CanvasHeight = (int)(HeightInput.Value ?? 1080),
                FPS = (int)(FpsInput.Value ?? 24)
            };
            
            // Add initial layer and frame
            var layer = new Layer { Name = "Layer 1" };
            layer.Frames.Add(new AnimationFrame { Index = 0 });
            project.Layers.Add(layer);

            OpenMainWindow(project);
        }

        private async void OpenProject_Click(object? sender, RoutedEventArgs e)
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Open Ersm Animation Project",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Ersm Animation Project") { Patterns = new[] { "*.ersma" } }
                }
            };

            var result = await StorageProvider.OpenFilePickerAsync(options);
            if (result.Count > 0)
            {
                var file = result[0];
                var project = await ProjectFileService.LoadProjectAsync(file.Path.LocalPath);
                if (project != null)
                {
                    OpenMainWindow(project);
                }
            }
        }

        private void OpenMainWindow(AnimationProject project)
        {
            var mainWindow = new MainWindow(project);
            mainWindow.Show();
            this.Close();
        }
    }
}
