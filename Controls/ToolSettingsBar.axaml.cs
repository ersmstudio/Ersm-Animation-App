using Avalonia.Controls;

namespace Ersm_Animation_App.Controls
{
    public partial class ToolSettingsBar : UserControl
    {
        public ToolSettingsBar()
        {
            InitializeComponent();
        }

        public void SetActiveTool(string toolName)
        {
            ToolNameText.Text = $"{toolName} Settings:";

            // Hide all
            BrushPanel.IsVisible = false;
            EraserPanel.IsVisible = false;
            TextPanel.IsVisible = false;
            SelectPanel.IsVisible = false;

            // Show relevant panel
            if (toolName == "Eraser")
            {
                EraserPanel.IsVisible = true;
            }
            else if (toolName == "Text")
            {
                TextPanel.IsVisible = true;
            }
            else if (toolName == "Select" || toolName == "Lasso")
            {
                ToolNameText.Text = $"{toolName}: Drag to select and transform";
                SelectPanel.IsVisible = true;
            }
            else if (toolName == "Bucket")
            {
                ToolNameText.Text = $"{toolName}: Click a shape to fill, or empty space for background";
            }
            else
            {
                // Default to brush properties (Pencil, Brush, Line, Rect, Ellipse)
                BrushPanel.IsVisible = true;
            }
        }
        
        public double GetThickness() => ThicknessSlider.Value;
        public double GetOpacity() => OpacitySlider.Value / 100.0;
        
        public double GetFontSize() => (double)(FontSizeInput.Value ?? 24);
        
        // 0 = Full, 1 = Path Cut, 2 = Node Cut
        public int GetEraserMode() => EraserModeCombo.SelectedIndex;
        public double GetEraserSize() => EraserSizeSlider.Value;

        // Events for Select Panel
        public event System.EventHandler? DeleteRequested;
        public event System.EventHandler? FlipHRequested;
        public event System.EventHandler? FlipVRequested;

        private void BtnSelectDelete_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => DeleteRequested?.Invoke(this, System.EventArgs.Empty);
        private void BtnSelectFlipH_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => FlipHRequested?.Invoke(this, System.EventArgs.Empty);
        private void BtnSelectFlipV_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => FlipVRequested?.Invoke(this, System.EventArgs.Empty);
    }
}
