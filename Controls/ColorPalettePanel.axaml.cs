using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Ersm_Animation_App.Models;

namespace Ersm_Animation_App.Controls
{
    public partial class ColorPalettePanel : UserControl
    {
        /// <summary>Fires when user selects a palette color. Carries the PaletteColor.</summary>
        public event Action<PaletteColor>? PaletteColorSelected;

        /// <summary>Fires when user changes a palette color (all strokes should update).</summary>
        public event Action<PaletteColor>? PaletteColorChanged;

        /// <summary>Fires when user deletes a palette color.</summary>
        public event Action<PaletteColor>? PaletteColorDeleted;

        private ColorPalette? _palette;
        private PaletteColor? _activeColor;

        public ColorPalettePanel()
        {
            InitializeComponent();
        }

        public void BindPalette(ColorPalette palette)
        {
            _palette = palette;
            RefreshPalette();

            // Auto-select first color
            if (_palette.Colors.Count > 0)
            {
                SelectColor(_palette.Colors[0]);
            }
        }

        public void RefreshPalette()
        {
            PaletteContainer.Children.Clear();
            if (_palette == null) return;

            foreach (var pc in _palette.Colors)
            {
                CreateColorButton(pc);
            }
        }

        private void CreateColorButton(PaletteColor pc)
        {
            var border = new Border
            {
                Width = 32,
                Height = 32,
                Background = new SolidColorBrush(pc.Color),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(3),
                BorderThickness = new Thickness(2),
                BorderBrush = Brushes.Transparent,
                Tag = pc,
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            // Tooltip shows name
            ToolTip.SetTip(border, $"{pc.Name}  (ID: {pc.Id})");

            // Left click = select
            border.PointerPressed += (s, e) =>
            {
                if (e.GetCurrentPoint(border).Properties.IsLeftButtonPressed)
                {
                    SelectColor(pc);
                    e.Handled = true;
                }
            };

            // Right click = delete
            border.PointerPressed += (s, e) =>
            {
                if (e.GetCurrentPoint(border).Properties.IsRightButtonPressed)
                {
                    DeleteColor(pc);
                    e.Handled = true;
                }
            };

            // Double click = edit color
            border.DoubleTapped += (s, e) =>
            {
                EditColor(pc, border);
            };

            PaletteContainer.Children.Add(border);
        }

        private void SelectColor(PaletteColor pc)
        {
            _activeColor = pc;
            ActiveColorPreview.Background = new SolidColorBrush(pc.Color);
            ActiveColorName.Text = pc.Name;

            // Highlight active
            foreach (var child in PaletteContainer.Children)
            {
                if (child is Border b)
                {
                    var bpc = b.Tag as PaletteColor;
                    b.BorderBrush = bpc?.Id == pc.Id
                        ? new SolidColorBrush(Color.FromRgb(70, 130, 230))
                        : Brushes.Transparent;
                }
            }

            PaletteColorSelected?.Invoke(pc);
        }

        private void DeleteColor(PaletteColor pc)
        {
            PaletteColorDeleted?.Invoke(pc);
            RefreshPalette();

            // If deleted color was active, select another
            if (_activeColor?.Id == pc.Id && _palette?.Colors.Count > 0)
            {
                SelectColor(_palette.Colors[0]);
            }
        }

        private void EditColor(PaletteColor pc, Border border)
        {
            // Cycle through a few preset colors for quick editing
            // In a full implementation, this would open a color picker dialog
            var presets = new[]
            {
                Colors.Black, Colors.White, Colors.Red, Colors.Blue,
                Colors.Green, Colors.Yellow, Color.FromRgb(245, 210, 180),
                Colors.Orange, Colors.Purple, Colors.Gray, Colors.Brown,
                Color.FromRgb(255, 105, 180), Color.FromRgb(0, 191, 255)
            };

            int currentIdx = -1;
            for (int i = 0; i < presets.Length; i++)
            {
                if (presets[i] == pc.Color) { currentIdx = i; break; }
            }

            int nextIdx = (currentIdx + 1) % presets.Length;
            pc.Color = presets[nextIdx];

            border.Background = new SolidColorBrush(pc.Color);
            if (_activeColor?.Id == pc.Id)
            {
                ActiveColorPreview.Background = new SolidColorBrush(pc.Color);
            }

            PaletteColorChanged?.Invoke(pc);
        }

        private void AddPaletteColor_Click(object? sender, RoutedEventArgs e)
        {
            if (_palette == null) return;

            // Add a new color with a unique name
            int idx = _palette.Colors.Count + 1;
            var pc = _palette.AddColor($"Color {idx}", Color.FromRgb(
                (byte)(100 + (idx * 37) % 155),
                (byte)(80 + (idx * 53) % 175),
                (byte)(60 + (idx * 71) % 195)));

            CreateColorButton(pc);
            SelectColor(pc);
        }

        public PaletteColor? ActiveColor => _activeColor;
    }
}
