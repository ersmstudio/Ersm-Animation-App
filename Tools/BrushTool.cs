using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Ersm_Animation_App.Models;

namespace Ersm_Animation_App.Tools
{
    public class BrushTool : ITool
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
                Thickness = canvas.CurrentThickness * 2.5,
                Opacity = canvas.CurrentOpacity * 0.7,
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
            _currentStroke.Points = PencilTool.SmoothPoints(_rawPoints);
            canvas.InvalidateVisual();
        }

        public void OnPointerReleased(DrawingCanvas canvas, PointerReleasedEventArgs e, AnimationFrame currentFrame)
        {
            if (_currentStroke != null && _rawPoints.Count > 1)
            {
                _currentStroke.Points = PencilTool.SmoothPoints(_rawPoints);
            }
            _currentStroke = null;
            _rawPoints.Clear();
        }

        public void Render(DrawingContext context, DrawingCanvas canvas) { }
    }
}
