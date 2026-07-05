using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Ersm_Animation_App.Controls
{
    public partial class CustomColorPicker : UserControl
    {
        public static readonly StyledProperty<Color> SelectedColorProperty =
            AvaloniaProperty.Register<CustomColorPicker, Color>(nameof(SelectedColor), Colors.Black);

        public Color SelectedColor
        {
            get => GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public event EventHandler<Color>? SelectedColorChanged;



        private readonly List<Color> presets = new()
        {
            Colors.Black, Colors.White, Colors.Red, Colors.Green, Colors.Blue,
            Color.Parse("#FFFFA500"), // Orange
            Color.Parse("#FF800080"), // Purple
            Color.Parse("#FF00FFFF")  // Cyan
        };

        public CustomColorPicker()
        {
            InitializeComponent();



            this.GetObservable(SelectedColorProperty).Subscribe(UpdateUIFromSelectedColor);

            if (RSlider != null) RSlider.GetObservable(RangeBase.ValueProperty).Subscribe(_ => UpdateColorFromSliders());
            if (GSlider != null) GSlider.GetObservable(RangeBase.ValueProperty).Subscribe(_ => UpdateColorFromSliders());
            if (BSlider != null) BSlider.GetObservable(RangeBase.ValueProperty).Subscribe(_ => UpdateColorFromSliders());
            if (ASlider != null) ASlider.GetObservable(RangeBase.ValueProperty).Subscribe(_ => UpdateColorFromSliders());

            if (HexTextBox != null)
            {
                HexTextBox.AddHandler(InputElement.LostFocusEvent, (s, e) => ParseHexAndApply(), RoutingStrategies.Tunnel);
                HexTextBox.KeyDown += HexTextBox_KeyDown;
            }

            if (OkButton != null) OkButton.Click += OkButton_Click;
            if (CancelButton != null) CancelButton.Click += CancelButton_Click;

            BuildPresets();

            UpdateUIFromSelectedColor(SelectedColor);
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void BuildPresets()
        {
            if (PresetsPanel == null) return;
            PresetsPanel.Children.Clear();
            foreach (var c in presets)
            {
                var b = new Border
                {
                    Width = 28,
                    Height = 28,
                    Background = new SolidColorBrush(c),
                    CornerRadius = new CornerRadius(4),
                    Tag = c
                };
                b.PointerPressed += (s, e) =>
                {
                    SelectedColor = (Color)((Border)s).Tag!;
                    SelectedColorChanged?.Invoke(this, SelectedColor);
                };
                PresetsPanel.Children.Add(b);
            }
        }

        private void UpdateColorFromSliders()
        {
            if (RSlider == null || GSlider == null || BSlider == null || ASlider == null)
                return;

            var a = (byte)Math.Clamp((int)ASlider.Value, 0, 255);
            var r = (byte)Math.Clamp((int)RSlider.Value, 0, 255);
            var g = (byte)Math.Clamp((int)GSlider.Value, 0, 255);
            var b = (byte)Math.Clamp((int)BSlider.Value, 0, 255);

            var color = Color.FromArgb(a, r, g, b);
            if (SelectedColor != color)
            {
                SelectedColor = color;
                SelectedColorChanged?.Invoke(this, color);
            }
        }

        private void UpdateUIFromSelectedColor(Color color)
        {
            if (PreviewBorder != null)
                PreviewBorder.Background = new SolidColorBrush(color);

            if (RSlider != null) RSlider.Value = color.R;
            if (GSlider != null) GSlider.Value = color.G;
            if (BSlider != null) BSlider.Value = color.B;
            if (ASlider != null) ASlider.Value = color.A;

            if (HexTextBox != null)
                HexTextBox.Text = ColorToHex(color);
        }

        private static string ColorToHex(Color c) =>
            $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

        private void ParseHexAndApply()
        {
            if (HexTextBox == null) return;
            var s = HexTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(s)) return;
            if (s.StartsWith("#")) s = s.Substring(1);

            try
            {
                Color parsed;
                if (s.Length == 6)
                {
                    var r = byte.Parse(s.Substring(0, 2), NumberStyles.HexNumber);
                    var g = byte.Parse(s.Substring(2, 2), NumberStyles.HexNumber);
                    var b = byte.Parse(s.Substring(4, 2), NumberStyles.HexNumber);
                    parsed = Color.FromArgb(255, r, g, b);
                }
                else if (s.Length == 8)
                {
                    var a = byte.Parse(s.Substring(0, 2), NumberStyles.HexNumber);
                    var r = byte.Parse(s.Substring(2, 2), NumberStyles.HexNumber);
                    var g = byte.Parse(s.Substring(4, 2), NumberStyles.HexNumber);
                    var b = byte.Parse(s.Substring(6, 2), NumberStyles.HexNumber);
                    parsed = Color.FromArgb(a, r, g, b);
                }
                else return;

                if (SelectedColor != parsed)
                {
                    SelectedColor = parsed;
                    SelectedColorChanged?.Invoke(this, parsed);
                }
            }
            catch { }
        }

        private void HexTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ParseHexAndApply();
        }

        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            // OK - keep SelectedColor and notify (already notified on change)
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            // optional rollback logic
        }
    }
}
