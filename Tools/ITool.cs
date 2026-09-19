using Avalonia.Input;
using Avalonia.Media;
using Ersm_Animation_App.Models;

namespace Ersm_Animation_App.Tools
{
    public interface ITool
    {
        void OnPointerPressed(DrawingCanvas canvas, PointerPressedEventArgs e, AnimationFrame currentFrame);
        void OnPointerMoved(DrawingCanvas canvas, PointerEventArgs e, AnimationFrame currentFrame);
        void OnPointerReleased(DrawingCanvas canvas, PointerReleasedEventArgs e, AnimationFrame currentFrame);
        void Render(DrawingContext context, DrawingCanvas canvas);
    }
}
