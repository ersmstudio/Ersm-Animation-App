using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Ersm_Animation_App.Models;

namespace Ersm_Animation_App.Tools
{
    public class TextTool : ITool
    {
        public async void OnPointerPressed(DrawingCanvas canvas, PointerPressedEventArgs e, AnimationFrame currentFrame)
        {
            if (!e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed) return;

            var insertPos = e.GetPosition(canvas);
            
            // Get the parent window to show the dialog
            var topLevel = TopLevel.GetTopLevel(canvas) as Window;
            if (topLevel == null) return;
            
            var dialog = new TextInputDialog();
            var result = await dialog.ShowDialog<string?>(topLevel);
            
            if (!string.IsNullOrWhiteSpace(result))
            {
                canvas.SaveUndoState();
                var textStroke = new Stroke
                {
                    Type = StrokeType.Text,
                    Text = result,
                    Color = canvas.CurrentColor,
                    Thickness = canvas.CurrentThickness * 4 > 12 ? canvas.CurrentThickness * 4 : 16,
                    Bounds = new Rect(insertPos, new Size(300, 50)),
                    Opacity = canvas.CurrentOpacity,
                    PaletteColorId = canvas.ActivePaletteColorId
                };
                currentFrame.Strokes.Add(textStroke);
                canvas.InvalidateVisual();
            }
        }

        public void OnPointerMoved(DrawingCanvas canvas, PointerEventArgs e, AnimationFrame currentFrame) { }
        public void OnPointerReleased(DrawingCanvas canvas, PointerReleasedEventArgs e, AnimationFrame currentFrame) { }
        public void Render(DrawingContext context, DrawingCanvas canvas) { }
    }
}
