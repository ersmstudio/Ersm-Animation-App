using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Ersm_Animation_App.Models;

namespace Ersm_Animation_App.Tools
{
    public class LassoTool : ITool
    {
        private List<Point> _lassoPoints = new();
        private bool _isDrawingLasso = false;

        public void OnPointerPressed(DrawingCanvas canvas, PointerPressedEventArgs e, AnimationFrame currentFrame)
        {
            if (!e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed) return;

            _isDrawingLasso = true;
            _lassoPoints.Clear();
            _lassoPoints.Add(e.GetPosition(canvas));

            // Deselect all initially
            foreach (var s in currentFrame.Strokes) s.Selected = false;
            
            canvas.InvalidateVisual();
        }

        public void OnPointerMoved(DrawingCanvas canvas, PointerEventArgs e, AnimationFrame currentFrame)
        {
            if (!_isDrawingLasso) return;
            
            _lassoPoints.Add(e.GetPosition(canvas));
            canvas.InvalidateVisual();
        }

        public void OnPointerReleased(DrawingCanvas canvas, PointerReleasedEventArgs e, AnimationFrame currentFrame)
        {
            if (!_isDrawingLasso) return;
            _isDrawingLasso = false;

            if (_lassoPoints.Count > 2)
            {
                // Find all strokes that intersect or are inside the polygon
                var bounds = GetPolygonBounds(_lassoPoints);
                foreach (var stroke in currentFrame.Strokes)
                {
                    if (!stroke.Visible) continue;

                    var sBounds = stroke.GetTransformedBounds();
                    // Simple check: if bounds intersect, consider it selected
                    if (bounds.Intersects(sBounds))
                    {
                        stroke.Selected = true;
                    }
                }
            }

            _lassoPoints.Clear();
            canvas.InvalidateVisual();
            
            // Switch to select tool so the user can manipulate the selection
            canvas.CurrentTools = MainWindow.ToolType.Select;
            // Note: Since SelectTool currently only supports transforming one stroke fully with handles,
            // multiple selection is mainly useful for deletion right now.
        }

        public void Render(DrawingContext context, DrawingCanvas canvas)
        {
            if (_isDrawingLasso && _lassoPoints.Count > 1)
            {
                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    ctx.BeginFigure(_lassoPoints[0], true); // true = closed
                    for (int i = 1; i < _lassoPoints.Count; i++)
                    {
                        ctx.LineTo(_lassoPoints[i]);
                    }
                }
                
                var brush = new SolidColorBrush(Colors.DodgerBlue, 0.2);
                var pen = new Pen(new SolidColorBrush(Colors.DodgerBlue), 1, new Avalonia.Media.DashStyle(new double[] { 4, 4 }, 0));
                context.DrawGeometry(brush, pen, geometry);
            }
        }

        private Rect GetPolygonBounds(List<Point> points)
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            foreach (var p in points)
            {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
