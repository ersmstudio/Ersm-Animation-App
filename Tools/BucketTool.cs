using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Ersm_Animation_App.Models;

namespace Ersm_Animation_App.Tools
{
    public class BucketTool : ITool
    {
        public void OnPointerPressed(DrawingCanvas canvas, PointerPressedEventArgs e, AnimationFrame currentFrame)
        {
            if (!e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed) return;

            var pos = e.GetPosition(canvas);

            // Try to find a shape stroke at the click location and change its fill
            for (int i = currentFrame.Strokes.Count - 1; i >= 0; i--)
            {
                var stroke = currentFrame.Strokes[i];
                if ((stroke.Type == StrokeType.Rectangle || stroke.Type == StrokeType.Ellipse)
                    && stroke.Bounds.Contains(pos))
                {
                    canvas.SaveUndoState();
                    stroke.FillColor = canvas.FillColor;
                    stroke.PaletteColorId = canvas.ActivePaletteColorId;
                    canvas.InvalidateVisual();
                    return;
                }
            }

            // If no shape found, create a fill rectangle at click position behind everything
            canvas.SaveUndoState();
            var fillStroke = new Stroke
            {
                Type = StrokeType.Rectangle,
                FillColor = canvas.FillColor,
                Color = Colors.Transparent,
                Thickness = 0,
                Opacity = canvas.CurrentOpacity,
                Bounds = new Rect(0, 0, canvas.Bounds.Width, canvas.Bounds.Height),
                PaletteColorId = canvas.ActivePaletteColorId
            };
            currentFrame.Strokes.Insert(0, fillStroke); // insert behind
            canvas.InvalidateVisual();
        }

        public void OnPointerMoved(DrawingCanvas canvas, PointerEventArgs e, AnimationFrame currentFrame) { }
        public void OnPointerReleased(DrawingCanvas canvas, PointerReleasedEventArgs e, AnimationFrame currentFrame) { }
        public void Render(DrawingContext context, DrawingCanvas canvas) { }
    }
}
