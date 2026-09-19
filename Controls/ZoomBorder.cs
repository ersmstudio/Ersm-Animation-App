using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;

namespace Ersm_Animation_App.Controls
{
    public class ZoomBorder : Border
    {
        private Point _origin;
        private Point _start;
        private bool _isPanning;

        public double ZoomSpeed { get; set; } = 1.2;
        public double MaxZoom { get; set; } = 10.0;
        public double MinZoom { get; set; } = 0.1;

        public ZoomBorder()
        {
            ClipToBounds = true;
            PointerWheelChanged += ZoomBorder_PointerWheelChanged;
            PointerPressed += ZoomBorder_PointerPressed;
            PointerReleased += ZoomBorder_PointerReleased;
            PointerMoved += ZoomBorder_PointerMoved;
        }

        public void Reset()
        {
            if (Child != null)
            {
                var st = GetMatrixTransform(Child);
                st.Matrix = Matrix.Identity;
                Child.InvalidateArrange();
            }
        }

        public void FitToScreen()
        {
            if (Child != null && Bounds.Width > 0 && Bounds.Height > 0 && Child.Bounds.Width > 0 && Child.Bounds.Height > 0)
            {
                double scaleX = Bounds.Width / Child.Bounds.Width;
                double scaleY = Bounds.Height / Child.Bounds.Height;
                double scale = Math.Min(scaleX, scaleY) * 0.9; // 90% fit
                if (scale <= 0) scale = 1.0;

                var st = GetMatrixTransform(Child);
                var mx = Matrix.CreateScale(scale, scale);
                // Center it
                double offsetX = (Bounds.Width - (Child.Bounds.Width * scale)) / 2;
                double offsetY = (Bounds.Height - (Child.Bounds.Height * scale)) / 2;
                mx = mx * Matrix.CreateTranslation(offsetX, offsetY);
                st.Matrix = mx;
                Child.InvalidateArrange();
            }
        }

        private void ZoomBorder_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (Child == null) return;
            if (e.KeyModifiers != KeyModifiers.Control) return; // Only zoom on Ctrl+Scroll

            var st = GetMatrixTransform(Child);
            var matrix = st.Matrix;

            double zoom = e.Delta.Y > 0 ? ZoomSpeed : 1 / ZoomSpeed;
            
            // Limit zoom
            if (matrix.M11 * zoom < MinZoom || matrix.M11 * zoom > MaxZoom) return;

            var pos = e.GetPosition(Child);
            
            matrix = matrix * Matrix.CreateTranslation(-pos.X, -pos.Y) 
                            * Matrix.CreateScale(zoom, zoom) 
                            * Matrix.CreateTranslation(pos.X, pos.Y);
            
            st.Matrix = matrix;
            Child.InvalidateArrange();
            e.Handled = true;
        }

        private void ZoomBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var props = e.GetCurrentPoint(this).Properties;
            if (props.IsMiddleButtonPressed)
            {
                if (Child != null)
                {
                    var st = GetMatrixTransform(Child);
                    _start = e.GetPosition(this);
                    _origin = new Point(st.Matrix.M31, st.Matrix.M32);
                    Cursor = new Cursor(StandardCursorType.Hand);
                    _isPanning = true;
                    e.Handled = true;
                }
            }
        }

        private void ZoomBorder_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isPanning)
            {
                Cursor = new Cursor(StandardCursorType.Arrow);
                _isPanning = false;
                e.Handled = true;
            }
        }

        private void ZoomBorder_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isPanning || Child == null) return;
            
            var st = GetMatrixTransform(Child);
            var matrix = st.Matrix;
            var pos = e.GetPosition(this);
            
            matrix = new Matrix(matrix.M11, matrix.M12, matrix.M21, matrix.M22, 
                                _origin.X + (pos.X - _start.X), 
                                _origin.Y + (pos.Y - _start.Y));
            
            st.Matrix = matrix;
            Child.InvalidateArrange();
            e.Handled = true;
        }

        private MatrixTransform GetMatrixTransform(Control element)
        {
            if (element.RenderTransform is not MatrixTransform mt)
            {
                mt = new MatrixTransform(Matrix.Identity);
                element.RenderTransform = mt;
            }
            return mt;
        }
    }
}
