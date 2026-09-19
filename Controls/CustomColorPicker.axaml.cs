using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Ersm_Animation_App.Controls
{
    public partial class CustomColorPicker : UserControl
    {
        private Color _tempColor;

        public event Action<Color>? ColorChanged;
        public event Action? CloseRequested;
        
        public static readonly StyledProperty<Color> SelectedColorProperty =
            AvaloniaProperty.Register<CustomColorPicker, Color>(nameof(SelectedColor), Colors.Black);

        public Color SelectedColor
        {
            get => GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public event EventHandler<Color>? SelectedColorChanged;

        private IDisposable? _selectedColorSub;
        private IDisposable? _rSub, _gSub, _bSub, _aSub;
        private bool _isUpdatingUI;

        public CustomColorPicker()
        {
            InitializeComponent();

            _selectedColorSub = this.GetObservable(SelectedColorProperty).Subscribe(UpdateUIFromSelectedColor);

            if (RSlider != null) _rSub = RSlider.GetObservable(RangeBase.ValueProperty).Subscribe(_ => UpdateColorFromSliders());
            if (GSlider != null) _gSub = GSlider.GetObservable(RangeBase.ValueProperty).Subscribe(_ => UpdateColorFromSliders());
            if (BSlider != null) _bSub = BSlider.GetObservable(RangeBase.ValueProperty).Subscribe(_ => UpdateColorFromSliders());
            if (ASlider != null) _aSub = ASlider.GetObservable(RangeBase.ValueProperty).Subscribe(_ => UpdateColorFromSliders());

            if (HexTextBox != null)
            {
                HexTextBox.AddHandler(InputElement.LostFocusEvent, (s, e) => ParseHexAndApply(), RoutingStrategies.Tunnel);
                HexTextBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) ParseHexAndApply(); };
            }

            var colorSquare = this.FindControl<ColorSquare>("ColorSquare");
            var colorSlider = this.FindControl<HueSlider>("ColorSlider");

            if (colorSquare != null)
            {
                colorSquare.ColorChanged += (color) =>
                {
                    if (SelectedColor != color)
                    {
                        SelectedColor = color;
                        SelectedColorChanged?.Invoke(this, color);
                    }
                };
            }

            if (colorSlider != null)
            {
                colorSlider.HueChanged += (hue) =>
                {
                    if (colorSquare != null)
                    {
                        colorSquare.Hue = hue;
                        var newColor = colorSquare.GetCurrentColor();
                        if (SelectedColor != newColor)
                        {
                            SelectedColor = newColor;
                            SelectedColorChanged?.Invoke(this, newColor);
                        }
                    }
                };
            }
        }

        public void BeginEdit(Color currentColor)
        {
            _tempColor = currentColor;
            SelectedColor = currentColor;
            UpdateUIFromSelectedColor(currentColor);
        }

        private void UpdateColorFromSliders()
        {
            if (_isUpdatingUI || RSlider == null || GSlider == null || BSlider == null || ASlider == null) return;

            var a = (byte)Math.Clamp((int)ASlider.Value, 0, 255);
            var r = (byte)Math.Clamp((int)RSlider.Value, 0, 255);
            var g = (byte)Math.Clamp((int)GSlider.Value, 0, 255);
            var b = (byte)Math.Clamp((int)BSlider.Value, 0, 255);

            var color = Color.FromArgb(a, r, g, b);
            if (SelectedColor != color)
            {
                SelectedColor = color;
                SelectedColorChanged?.Invoke(this, color);
                ColorChanged?.Invoke(SelectedColor);
            }
        }

        private void UpdateUIFromSelectedColor(Color color)
        {
            if (_isUpdatingUI) return;
            _isUpdatingUI = true;

            try
            {
                if (PreviewBorder != null) PreviewBorder.Background = new SolidColorBrush(color);
                if (HexDisplay != null) HexDisplay.Text = ColorToHex(color);
                if (RgbDisplay != null) RgbDisplay.Text = $"RGB({color.R}, {color.G}, {color.B})";

                if (RSlider != null) RSlider.Value = color.R;
                if (GSlider != null) GSlider.Value = color.G;
                if (BSlider != null) BSlider.Value = color.B;
                if (ASlider != null) ASlider.Value = color.A;

                if (this.FindControl<TextBlock>("RValue") is { } rVal) rVal.Text = color.R.ToString();
                if (this.FindControl<TextBlock>("GValue") is { } gVal) gVal.Text = color.G.ToString();
                if (this.FindControl<TextBlock>("BValue") is { } bVal) bVal.Text = color.B.ToString();
                if (this.FindControl<TextBlock>("AValue") is { } aVal) aVal.Text = color.A.ToString();

                if (HexTextBox != null) HexTextBox.Text = ColorToHex(color);

                if (this.FindControl<ColorSquare>("ColorSquare") is { } cs)
                {
                    cs.SetColorFromExternal(color);
                    if (this.FindControl<HueSlider>("ColorSlider") is { } hs && cs.Hue >= 0)
                    {
                        hs.SetHueFromExternal(cs.Hue);
                    }
                }
            }
            finally { _isUpdatingUI = false; }
        }

        private static string ColorToHex(Color c) => $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

        private void ParseHexAndApply()
        {
            var s = HexTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(s)) return;
            if (s.StartsWith("#")) s = s.Substring(1);
            try
            {
                Color parsed = Colors.Black;
                if (s.Length == 6)
                {
                    parsed = Color.FromArgb(255, byte.Parse(s.Substring(0, 2), NumberStyles.HexNumber),
                        byte.Parse(s.Substring(2, 2), NumberStyles.HexNumber),
                        byte.Parse(s.Substring(4, 2), NumberStyles.HexNumber));
                }
                else if (s.Length == 8)
                {
                    parsed = Color.FromArgb(byte.Parse(s.Substring(0, 2), NumberStyles.HexNumber),
                        byte.Parse(s.Substring(2, 2), NumberStyles.HexNumber),
                        byte.Parse(s.Substring(4, 2), NumberStyles.HexNumber),
                        byte.Parse(s.Substring(6, 2), NumberStyles.HexNumber));
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

        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            ColorChanged?.Invoke(SelectedColor);
            CloseRequested?.Invoke();
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            SelectedColor = _tempColor;
            UpdateUIFromSelectedColor(_tempColor);
            CloseRequested?.Invoke();
        }
    }
}
