using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;

namespace Ersm_Animation_App.Controls
{
    public class ColorSquare : Control
    {
        public double SelectedSaturation { get; private set; }
        public double SelectedValue { get; private set; }
        public event Action<Color>? ColorChanged;
        private double _hue;
        private bool _isUpdating;

        public void SetColorFromExternal(Color color)
        {
            if (_isUpdating) return;
            _isUpdating = true;
            
            ColorToHsv(color, out double h, out double s, out double v);
            
            if (s > 0.001 && v > 0.001)
                Hue = h;

            SelectedSaturation = s;
            SelectedValue = v;

            InvalidateVisual();
            _isUpdating = false;
        }

        private static void ColorToHsv(Color color, out double h, out double s, out double v)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            v = max;
            s = max == 0 ? 0 : delta / max;
            h = 0;

            if (delta == 0)
                h = 0;
            else if (max == r)
                h = 60 * (((g - b) / delta) % 6);
            else if (max == g)
                h = 60 * (((b - r) / delta) + 2);
            else if (max == b)
                h = 60 * (((r - g) / delta) + 4);

            if (h < 0) h += 360;
        }

        public double Hue
        {
            get => _hue;
            set
            {
                _hue = value;
                GenerateColorSquare();
                InvalidateVisual();
            }
        }

        private WriteableBitmap _bitmap =
            new WriteableBitmap(
                new PixelSize(256, 256),
                new Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888,
                Avalonia.Platform.AlphaFormat.Premul);

        private Color HsvToColor(double hue, double saturation, double value)
        {
            double c = value * saturation;
            double x = c * (1 - Math.Abs((hue / 60.0) % 2 - 1));
            double m = value - c;

            double r, g, b;

            if (hue < 60) { r = c; g = x; b = 0; }
            else if (hue < 120) { r = x; g = c; b = 0; }
            else if (hue < 180) { r = 0; g = c; b = x; }
            else if (hue < 240) { r = 0; g = x; b = c; }
            else if (hue < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return Color.FromRgb((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
        }

        void GenerateColorSquare()
        {
            int width = _bitmap.PixelSize.Width;
            int height = _bitmap.PixelSize.Height;
            using var fb = _bitmap.Lock();
            unsafe
            {
                byte* buffer = (byte*)fb.Address;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        double saturation = (double)x / (width - 1);
                        double value = 1.0 - ((double)y / (height - 1));
                        Color color = HsvToColor(Hue, saturation, value);

                        int pixelIndex = (y * fb.RowBytes) + (x * 4);
                        buffer[pixelIndex + 0] = color.B;
                        buffer[pixelIndex + 1] = color.G;
                        buffer[pixelIndex + 2] = color.R;
                        buffer[pixelIndex + 3] = color.A;
                    }
                }
            }
        }

        private void OnPointer(object? sender, PointerEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                return;

            // Guard against zero-size bounds to prevent NaN/crash
            if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

            var pos = e.GetPosition(this);

            SelectedSaturation = Math.Clamp(pos.X / Bounds.Width, 0, 1);
            SelectedValue = Math.Clamp(1 - (pos.Y / Bounds.Height), 0, 1);

            var color = HsvToColor(Hue, SelectedSaturation, SelectedValue);
            ColorChanged?.Invoke(color);
            InvalidateVisual();
        }

        public ColorSquare()
        {
            GenerateColorSquare();
            PointerPressed += OnPointer;
            PointerMoved += OnPointer;
        }

        public Color GetCurrentColor()
        {
            return HsvToColor(Hue, SelectedSaturation, SelectedValue);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            context.DrawImage(
                _bitmap,
                new Rect(0, 0, _bitmap.PixelSize.Width, _bitmap.PixelSize.Height),
                new Rect(0, 0, Bounds.Width, Bounds.Height));

            var selectorPos = new Point(
                SelectedSaturation * Bounds.Width,
                (1 - SelectedValue) * Bounds.Height
            );

            context.DrawEllipse(
                null,
                new Pen(Brushes.White, 2),
                selectorPos,
                6,
                6);
        }
    }
}