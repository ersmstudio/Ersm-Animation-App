using System;
using System.Linq;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Ersm_Animation_App.Models;
using Avalonia.Controls;

namespace Ersm_Animation_App.Tools
{
    public class SelectTool : ITool
    {
        private enum TransformMode { None, Translate, Scale, Rotate, MovePivot, Skew }
        
        private TransformMode _currentMode = TransformMode.None;
        private Point _lastMousePos;
        private Stroke? _selectedStroke;
        private int _scaleHandleIndex = -1; // 0-7 clockwise from top-left

        private const double HandleSize = 10;
        private const double HitTolerance = 5.0;

        public void OnPointerPressed(DrawingCanvas canvas, PointerPressedEventArgs e, AnimationFrame currentFrame)
        {
            var point = e.GetPosition(canvas);
            var props = e.GetCurrentPoint(canvas).Properties;

            // Handle Context Menu (Right Click)
            if (props.IsRightButtonPressed)
            {
                if (_selectedStroke != null && _selectedStroke.HitTestPoint(point, HitTolerance))
                {
                    // Flip image on right click as requested
                    _selectedStroke.FlipX = !_selectedStroke.FlipX;
                    canvas.InvalidateVisual();
                }
                return;
            }

            if (!props.IsLeftButtonPressed) return;

            // 1. Check if we clicked a transform handle of the currently selected stroke
            if (_selectedStroke != null)
            {
                var handles = GetHandles(_selectedStroke);
                for (int i = 0; i < handles.Length; i++)
                {
                    if (new Rect(handles[i].X - HandleSize, handles[i].Y - HandleSize, HandleSize * 2, HandleSize * 2).Contains(point))
                    {
                        canvas.SaveUndoState();
                        // If it's an edge handle (1, 3, 5, 7) and Ctrl is pressed -> Skew
                        if ((i % 2 != 0) && (e.KeyModifiers & KeyModifiers.Control) != 0)
                            _currentMode = TransformMode.Skew;
                        else
                            _currentMode = TransformMode.Scale;
                        
                        _scaleHandleIndex = i;
                        _lastMousePos = point;
                        return;
                    }
                }

                // Check Rotation Handle (above top center)
                var rotHandle = GetRotationHandle(_selectedStroke);
                if (new Rect(rotHandle.X - HandleSize, rotHandle.Y - HandleSize, HandleSize * 2, HandleSize * 2).Contains(point))
                {
                    canvas.SaveUndoState();
                    _currentMode = TransformMode.Rotate;
                    _lastMousePos = point;
                    return;
                }

                // Check Pivot Move
                var pivotScreen = _selectedStroke.GetPivot().Transform(_selectedStroke.GetRenderMatrix());
                if (new Rect(pivotScreen.X - HandleSize, pivotScreen.Y - HandleSize, HandleSize * 2, HandleSize * 2).Contains(point))
                {
                    canvas.SaveUndoState();
                    _currentMode = TransformMode.MovePivot;
                    _lastMousePos = point;
                    return;
                }
            }

            // 2. Hit test strokes in reverse order (top to bottom)
            Stroke? hit = null;
            for (int i = currentFrame.Strokes.Count - 1; i >= 0; i--)
            {
                var s = currentFrame.Strokes[i];
                if (s.HitTestPoint(point, HitTolerance))
                {
                    hit = s;
                    break;
                }
            }

            // Deselect all
            foreach (var s in currentFrame.Strokes) s.Selected = false;

            if (hit != null)
            {
                hit.Selected = true;
                _selectedStroke = hit;
                _currentMode = TransformMode.Translate;
                canvas.SaveUndoState();
            }
            else
            {
                _selectedStroke = null;
                _currentMode = TransformMode.None;
            }

            _lastMousePos = point;
            canvas.InvalidateVisual();
        }

        public void OnPointerMoved(DrawingCanvas canvas, PointerEventArgs e, AnimationFrame currentFrame)
        {
            if (_selectedStroke == null || _currentMode == TransformMode.None) return;

            var point = e.GetPosition(canvas);
            double dx = point.X - _lastMousePos.X;
            double dy = point.Y - _lastMousePos.Y;
            var mods = e.KeyModifiers;
            bool isAlt = (mods & KeyModifiers.Alt) != 0;
            bool isShift = (mods & KeyModifiers.Shift) != 0;

            if (_currentMode == TransformMode.Translate)
            {
                _selectedStroke.TranslateX += dx;
                _selectedStroke.TranslateY += dy;
            }
            else if (_currentMode == TransformMode.Rotate)
            {
                var pivot = _selectedStroke.GetPivot().Transform(_selectedStroke.GetRenderMatrix());
                var angle1 = Math.Atan2(_lastMousePos.Y - pivot.Y, _lastMousePos.X - pivot.X);
                var angle2 = Math.Atan2(point.Y - pivot.Y, point.X - pivot.X);
                _selectedStroke.Rotation += (angle2 - angle1) * 180.0 / Math.PI;
            }
            else if (_currentMode == TransformMode.MovePivot)
            {
                var matrix = _selectedStroke.GetRenderMatrix();
                if (matrix.HasInverse)
                {
                    var p1 = _lastMousePos.Transform(matrix.Invert());
                    var p2 = point.Transform(matrix.Invert());
                    var local = _selectedStroke.GetPivot();
                    _selectedStroke.Pivot = new Point(local.X + (p2.X - p1.X), local.Y + (p2.Y - p1.Y));
                }
            }
            else if (_currentMode == TransformMode.Skew)
            {
                if (_scaleHandleIndex == 1 || _scaleHandleIndex == 5) // Top/Bottom -> SkewX
                    _selectedStroke.SkewX += dx * 0.5;
                else if (_scaleHandleIndex == 3 || _scaleHandleIndex == 7) // Left/Right -> SkewY
                    _selectedStroke.SkewY += dy * 0.5;
            }
            else if (_currentMode == TransformMode.Scale)
            {
                // To keep it robust without matrix decomposition, we calculate scale delta
                var matrix = _selectedStroke.GetRenderMatrix();
                if (matrix.HasInverse)
                {
                    var p1 = _lastMousePos.Transform(matrix.Invert());
                    var p2 = point.Transform(matrix.Invert());
                    
                    double ldx = p2.X - p1.X;
                    double ldy = p2.Y - p1.Y;
                    var b = _selectedStroke.GetLocalBounds();
                    
                    double scaleXDelta = ldx / b.Width;
                    double scaleYDelta = ldy / b.Height;
                    
                    // Adjust sign based on handle position
                    if (_scaleHandleIndex == 0 || _scaleHandleIndex == 6 || _scaleHandleIndex == 7) scaleXDelta = -scaleXDelta;
                    if (_scaleHandleIndex == 0 || _scaleHandleIndex == 1 || _scaleHandleIndex == 2) scaleYDelta = -scaleYDelta;
                    
                    if (_scaleHandleIndex == 1 || _scaleHandleIndex == 5) scaleXDelta = 0; // Top/Bottom center
                    if (_scaleHandleIndex == 3 || _scaleHandleIndex == 7) scaleYDelta = 0; // Left/Right center

                    // Uniform scaling (Shift)
                    if (isShift)
                    {
                        if (Math.Abs(scaleXDelta) > Math.Abs(scaleYDelta)) scaleYDelta = scaleXDelta;
                        else scaleXDelta = scaleYDelta;
                    }

                    // Alt modifier: Scale from center vs opposite corner
                    double factorX = isAlt ? 2.0 : 1.0;
                    double factorY = isAlt ? 2.0 : 1.0;

                    _selectedStroke.ScaleX += scaleXDelta * factorX;
                    _selectedStroke.ScaleY += scaleYDelta * factorY;
                    
                    // Note: True anchor-based scaling would require adjusting TranslateX/Y.
                    // For now, this modifies scale directly which centers around the pivot.
                    // If Alt is NOT pressed, we compensate translation to simulate opposite corner anchor.
                    if (!isAlt)
                    {
                        // Simplified compensation:
                        _selectedStroke.TranslateX += dx * 0.5;
                        _selectedStroke.TranslateY += dy * 0.5;
                    }
                }
            }

            _lastMousePos = point;
            canvas.InvalidateVisual();
        }

        public void OnPointerReleased(DrawingCanvas canvas, PointerReleasedEventArgs e, AnimationFrame currentFrame)
        {
            _currentMode = TransformMode.None;
        }

        public void Render(DrawingContext context, DrawingCanvas canvas)
        {
            if (_selectedStroke == null) return;

            var bounds = _selectedStroke.GetTransformedBounds();
            var handleBrush = new SolidColorBrush(Colors.White);
            var handlePen = new Pen(new SolidColorBrush(Colors.DodgerBlue), 1);
            var boxPen = new Pen(new SolidColorBrush(Colors.DodgerBlue), 1);

            // Draw bounding box
            context.DrawRectangle(null, boxPen, bounds);

            // Draw 8 resize handles
            var handles = GetHandlesBounds(bounds);
            foreach (var h in handles)
            {
                context.DrawRectangle(handleBrush, handlePen, new Rect(h.X - HandleSize / 2, h.Y - HandleSize / 2, HandleSize, HandleSize));
            }

            // Draw Rotation Handle
            var rot = new Point(bounds.Center.X, bounds.Top - 30);
            context.DrawLine(boxPen, new Point(bounds.Center.X, bounds.Top), rot);
            context.DrawEllipse(handleBrush, handlePen, rot, HandleSize / 2, HandleSize / 2);

            // Draw Pivot
            var pivotScreen = _selectedStroke.GetPivot().Transform(_selectedStroke.GetRenderMatrix());
            context.DrawEllipse(new SolidColorBrush(Colors.Red), null, pivotScreen, 4, 4);
        }

        private Point[] GetHandles(Stroke stroke) => GetHandlesBounds(stroke.GetTransformedBounds());

        private Point[] GetHandlesBounds(Rect b)
        {
            return new[]
            {
                b.TopLeft, new Point(b.Center.X, b.Top), b.TopRight,
                new Point(b.Right, b.Center.Y), b.BottomRight, new Point(b.Center.X, b.Bottom),
                b.BottomLeft, new Point(b.Left, b.Center.Y)
            };
        }

        private Point GetRotationHandle(Stroke stroke)
        {
            var b = stroke.GetTransformedBounds();
            return new Point(b.Center.X, b.Top - 30);
        }
    }
}
