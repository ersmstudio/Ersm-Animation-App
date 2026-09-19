using System.Linq;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Ersm_Animation_App.Models;

namespace Ersm_Animation_App.Tools
{
    public class EraserTool : ITool
    {
        private bool _isErasing;
        private Point _lastPoint;

        public void OnPointerPressed(DrawingCanvas canvas, PointerPressedEventArgs e, AnimationFrame currentFrame)
        {
            if (!e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed) return;
            
            canvas.SaveUndoState();
            _isErasing = true;
            _lastPoint = e.GetPosition(canvas);
            
            EraseAtPoint(canvas, _lastPoint, currentFrame);
        }

        public void OnPointerMoved(DrawingCanvas canvas, PointerEventArgs e, AnimationFrame currentFrame)
        {
            if (!_isErasing) return;
            
            _lastPoint = e.GetPosition(canvas);
            EraseAtPoint(canvas, _lastPoint, currentFrame);
        }

        public void OnPointerReleased(DrawingCanvas canvas, PointerReleasedEventArgs e, AnimationFrame currentFrame)
        {
            _isErasing = false;
            canvas.InvalidateVisual();
        }

        private void EraseAtPoint(DrawingCanvas canvas, Point p, AnimationFrame frame)
        {
            bool modified = false;
            double radius = canvas.CurrentEraserSize;
            int mode = canvas.CurrentEraserMode; // 0 = Full, 1 = Path Cut, 2 = Node Cut

            if (mode == 0) // Full Stroke
            {
                for (int i = frame.Strokes.Count - 1; i >= 0; i--)
                {
                    if (frame.Strokes[i].HitTestPoint(p, radius))
                    {
                        frame.Strokes.RemoveAt(i);
                        modified = true;
                    }
                }
            }
            else if (mode == 1) // Path Cut (Split)
            {
                for (int i = frame.Strokes.Count - 1; i >= 0; i--)
                {
                    var stroke = frame.Strokes[i];
                    if (stroke.Type != StrokeType.Freehand && stroke.Type != StrokeType.Line) continue;
                    
                    var matrix = stroke.GetRenderMatrix();
                    if (!matrix.HasInverse) continue;
                    var localPoint = p.Transform(matrix.Invert());
                    
                    // Find sequences of points that are OUTSIDE the eraser radius
                    var newStrokes = new List<Stroke>();
                    var currentSegment = new List<Point>();
                    
                    double radSq = radius * radius;
                    
                    foreach (var pt in stroke.Points)
                    {
                        if (Dist2(pt, localPoint) > radSq)
                        {
                            currentSegment.Add(pt);
                        }
                        else
                        {
                            // Hit eraser! Finish current segment if it has points
                            if (currentSegment.Count > 1)
                            {
                                var newStroke = stroke.Clone();
                                newStroke.Points = new List<Point>(currentSegment);
                                newStrokes.Add(newStroke);
                            }
                            currentSegment.Clear();
                        }
                    }
                    
                    if (currentSegment.Count > 1 && newStrokes.Count > 0)
                    {
                        // Add the final segment if there was a cut
                        var newStroke = stroke.Clone();
                        newStroke.Points = new List<Point>(currentSegment);
                        newStrokes.Add(newStroke);
                    }
                    
                    if (newStrokes.Count > 0 && currentSegment.Count != stroke.Points.Count)
                    {
                        // Stroke was cut! Replace it
                        frame.Strokes.RemoveAt(i);
                        frame.Strokes.InsertRange(i, newStrokes);
                        modified = true;
                    }
                    else if (currentSegment.Count <= 1)
                    {
                        // Whole stroke was erased
                        frame.Strokes.RemoveAt(i);
                        modified = true;
                    }
                }
            }
            else if (mode == 2) // Node Cut
            {
                for (int i = frame.Strokes.Count - 1; i >= 0; i--)
                {
                    var stroke = frame.Strokes[i];
                    if (stroke.Type != StrokeType.Freehand && stroke.Type != StrokeType.Line) continue;
                    
                    var matrix = stroke.GetRenderMatrix();
                    if (!matrix.HasInverse) continue;
                    var localPoint = p.Transform(matrix.Invert());
                    
                    double radSq = radius * radius;
                    int removed = stroke.Points.RemoveAll(pt => Dist2(pt, localPoint) <= radSq);
                    
                    if (removed > 0)
                    {
                        modified = true;
                        if (stroke.Points.Count < 2)
                            frame.Strokes.RemoveAt(i); // Delete if useless
                    }
                }
            }

            if (modified) canvas.InvalidateVisual();
        }

        private double Dist2(Point p1, Point p2)
        {
            var dx = p1.X - p2.X;
            var dy = p1.Y - p2.Y;
            return dx * dx + dy * dy;
        }

        public void Render(DrawingContext context, DrawingCanvas canvas)
        {
            if (_isErasing)
            {
                var brush = new SolidColorBrush(Colors.LightGray, 0.5);
                var pen = new Pen(Brushes.Black, 1);
                context.DrawEllipse(brush, pen, _lastPoint, canvas.CurrentEraserSize, canvas.CurrentEraserSize);
            }
        }
    }
}
