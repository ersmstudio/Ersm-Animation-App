using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Ersm_Animation_App.Models;

namespace Ersm_Animation_App.Tools
{
    public class PencilTool : ITool
    {
        private Stroke? _currentStroke;
        private List<Point> _rawPoints = new();

        public void OnPointerPressed(DrawingCanvas canvas, PointerPressedEventArgs e, AnimationFrame currentFrame)
        {
            if (!e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed) return;
            canvas.SaveUndoState();
            _rawPoints.Clear();
            var pos = e.GetPosition(canvas);
            _rawPoints.Add(pos);

            _currentStroke = new Stroke
            {
                Color = canvas.CurrentColor,
                Thickness = canvas.CurrentThickness,
                Opacity = canvas.CurrentOpacity,
                PaletteColorId = canvas.ActivePaletteColorId
            };
            _currentStroke.Points.Add(pos);
            currentFrame.Strokes.Add(_currentStroke);
            canvas.InvalidateVisual();
        }

        public void OnPointerMoved(DrawingCanvas canvas, PointerEventArgs e, AnimationFrame currentFrame)
        {
            if (_currentStroke == null || !e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed)
                return;

            var pos = e.GetPosition(canvas);
            _rawPoints.Add(pos);

            // Rebuild smoothed points
            _currentStroke.Points = SmoothPoints(_rawPoints);
            canvas.InvalidateVisual();
        }

        public void OnPointerReleased(DrawingCanvas canvas, PointerReleasedEventArgs e, AnimationFrame currentFrame)
        {
            if (_currentStroke != null && _rawPoints.Count > 1)
            {
                _currentStroke.Points = SmoothPoints(_rawPoints);
            }
            _currentStroke = null;
            _rawPoints.Clear();
        }

        public void Render(DrawingContext context, DrawingCanvas canvas) { }

        /// <summary>
        /// Catmull-Rom spline interpolation for smooth curves
        /// </summary>
        public static List<Point> SmoothPoints(List<Point> raw)
        {
            if (raw.Count < 3) return new List<Point>(raw);

            var result = new List<Point>();
            int segments = 6; // interpolation points between each pair

            for (int i = 0; i < raw.Count - 1; i++)
            {
                var p0 = raw[Math.Max(i - 1, 0)];
                var p1 = raw[i];
                var p2 = raw[Math.Min(i + 1, raw.Count - 1)];
                var p3 = raw[Math.Min(i + 2, raw.Count - 1)];

                for (int j = 0; j < segments; j++)
                {
                    double t = j / (double)segments;
                    double t2 = t * t;
                    double t3 = t2 * t;

                    double x = 0.5 * ((2 * p1.X) +
                        (-p0.X + p2.X) * t +
                        (2 * p0.X - 5 * p1.X + 4 * p2.X - p3.X) * t2 +
                        (-p0.X + 3 * p1.X - 3 * p2.X + p3.X) * t3);

                    double y = 0.5 * ((2 * p1.Y) +
                        (-p0.Y + p2.Y) * t +
                        (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2 +
                        (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * t3);

                    result.Add(new Point(x, y));
                }
            }
            result.Add(raw[^1]);
            return result;
        }
    }
}
