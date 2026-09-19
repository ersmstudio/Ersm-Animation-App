using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace Ersm_Animation_App.Controls
{
    public partial class SwatchesPanel : UserControl
    {
        public event Action<Color>? SwatchSelected;
        public event Action? RequestAddSwatch;

        public SwatchesPanel()
        {
            InitializeComponent();
        }

        public void LoadSwatches(List<string> swatches)
        {
            PresetsPanel.Children.Clear();
            foreach (var hex in swatches)
            {
                if (Color.TryParse(hex, out var color))
                {
                    AddSwatchButton(color);
                }
            }
        }

        public void AddSwatchButton(Color color)
        {
            var b = new Border
            {
                Width = 28, Height = 28,
                Background = new SolidColorBrush(color),
                CornerRadius = new Avalonia.CornerRadius(4),
                Margin = new Avalonia.Thickness(2)
            };
            b.PointerPressed += (s, e) => SwatchSelected?.Invoke(color);
            PresetsPanel.Children.Add(b);
        }

        private void AddSwatch_Click(object? sender, RoutedEventArgs e)
        {
            RequestAddSwatch?.Invoke();
        }
    }
}
