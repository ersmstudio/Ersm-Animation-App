using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Ersm_Animation_App.Models;

namespace Ersm_Animation_App.Tools
{
    public class EllipseTool : ITool
    {
        private Stroke? _currentStroke;
        private Point _startPoint;

        public void OnPointerPressed(DrawingCanvas canvas, PointerPressedEventArgs e, AnimationFrame currentFrame)
        {
            if (!e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed) return;

            canvas.SaveUndoState();
            _startPoint = e.GetPosition(canvas);
            
            _currentStroke = new Stroke
            {
                Type = StrokeType.Ellipse,
                Color = canvas.CurrentColor,
                FillColor = canvas.FillColor,
                Thickness = canvas.CurrentThickness,
                Opacity = canvas.CurrentOpacity,
                Bounds = new Rect(_startPoint, _startPoint)
            };
            
            currentFrame.Strokes.Add(_currentStroke);
            canvas.InvalidateVisual();
        }

        public void OnPointerMoved(DrawingCanvas canvas, PointerEventArgs e, AnimationFrame currentFrame)
        {
            if (_currentStroke == null || !e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed)
                return;

            var currentPoint = e.GetPosition(canvas);
            
            double x = System.Math.Min(_startPoint.X, currentPoint.X);
            double y = System.Math.Min(_startPoint.Y, currentPoint.Y);
            double width = System.Math.Abs(_startPoint.X - currentPoint.X);
            double height = System.Math.Abs(_startPoint.Y - currentPoint.Y);
            
            // Shift x,y to center because we want to draw ellipse based on bounding box
            _currentStroke.Bounds = new Rect(x, y, width, height);
            canvas.InvalidateVisual();
        }

        public void OnPointerReleased(DrawingCanvas canvas, PointerReleasedEventArgs e, AnimationFrame currentFrame)
        {
            if (_currentStroke != null)
            {
                if (_currentStroke.Bounds.Width == 0 && _currentStroke.Bounds.Height == 0)
                {
                    currentFrame.Strokes.Remove(_currentStroke);
                }
                _currentStroke = null;
                canvas.InvalidateVisual();
            }
        }

        public void Render(DrawingContext context, DrawingCanvas canvas)
        {
        }
    }
}
