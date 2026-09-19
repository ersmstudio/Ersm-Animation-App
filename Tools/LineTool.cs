using Avalonia.Input;
using Avalonia.Media;
using Ersm_Animation_App.Models;

namespace Ersm_Animation_App.Tools
{
    public class LineTool : ITool
    {
        private Stroke? _currentStroke;

        public void OnPointerPressed(DrawingCanvas canvas, PointerPressedEventArgs e, AnimationFrame currentFrame)
        {
            if (!e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed) return;

            canvas.SaveUndoState();
            var pos = e.GetPosition(canvas);
            
            _currentStroke = new Stroke
            {
                Type = StrokeType.Line,
                Color = canvas.CurrentColor,
                Thickness = canvas.CurrentThickness,
                Opacity = canvas.CurrentOpacity
            };
            
            _currentStroke.Points.Add(pos);
            _currentStroke.Points.Add(pos); // Two points for a line
            
            currentFrame.Strokes.Add(_currentStroke);
            canvas.InvalidateVisual();
        }

        public void OnPointerMoved(DrawingCanvas canvas, PointerEventArgs e, AnimationFrame currentFrame)
        {
            if (_currentStroke == null || !e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed)
                return;

            _currentStroke.Points[1] = e.GetPosition(canvas); // Update the end point
            canvas.InvalidateVisual();
        }

        public void OnPointerReleased(DrawingCanvas canvas, PointerReleasedEventArgs e, AnimationFrame currentFrame)
        {
            if (_currentStroke != null)
            {
                // If it's too small, remove it
                if (_currentStroke.Points[0] == _currentStroke.Points[1])
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
