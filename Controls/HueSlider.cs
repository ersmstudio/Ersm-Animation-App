using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;

namespace Ersm_Animation_App.Controls
{
    public class HueSlider : Control
    {
        public double SelectedHue { get; private set; }
        public event Action<double>? HueChanged;
        private WriteableBitmap _bitmap =
            new WriteableBitmap(
                new PixelSize(32, 360),
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

        public void SetHueFromExternal(double hue)
        {
            SelectedHue = hue;
            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            context.DrawImage(
                _bitmap,
                new Rect(0, 0, _bitmap.PixelSize.Width, _bitmap.PixelSize.Height),
                new Rect(0, 0, Bounds.Width, Bounds.Height));

            double selectorY = (SelectedHue / 360.0) * Bounds.Height;

            context.DrawLine(
                new Pen(Brushes.White, 2),
                new Point(0, selectorY),
                new Point(Bounds.Width, selectorY));
        }

        private void OnPointer(object? sender, PointerEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                return;

            // Guard against zero-size bounds
            if (Bounds.Height <= 0) return;

            var pos = e.GetPosition(this);

            double hue = Math.Clamp(pos.Y / Bounds.Height * 360.0, 0, 360);

            SelectedHue = hue;
            HueChanged?.Invoke(hue);
            InvalidateVisual();
        }

        public HueSlider()
        {
            GenerateHueBitmap();
            PointerPressed += OnPointer;
            PointerMoved += OnPointer;
        }

        private void GenerateHueBitmap()
        {
            using var fb = _bitmap.Lock();

            unsafe
            {
                byte* buffer = (byte*)fb.Address;
                for (int y = 0; y < 360; y++)
                {
                    double hue = (double)y / (_bitmap.PixelSize.Height - 1) * 360.0;

                    var color = HsvToColor(hue, 1, 1);
                    for (int x = 0; x < 32; x++)
                    {
                        int pixelIndex = (y * fb.RowBytes) + (x * 4);
                        buffer[pixelIndex + 0] = color.B;
                        buffer[pixelIndex + 1] = color.G;
                        buffer[pixelIndex + 2] = color.R;
                        buffer[pixelIndex + 3] = color.A;
                    }
                }
            }
        }
    }
}