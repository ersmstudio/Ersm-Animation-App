using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;

namespace Ersm_Animation_App.Models
{
    public enum StrokeType { Freehand, Line, Rectangle, Ellipse, Text }

    public class Stroke
    {
        public Guid Id { get; } = Guid.NewGuid();
        public StrokeType Type { get; set; } = StrokeType.Freehand;
        public List<Point> Points { get; set; } = new();
        
        // Stroke
        public Color Color { get; set; } = Colors.Black;
        public double Thickness { get; set; } = 2.0;
        public double Opacity { get; set; } = 1.0;
        
        // Fill & Shape
        public Color FillColor { get; set; } = Colors.Transparent;
        public Rect Bounds { get; set; }
        
        // Text
        public string Text { get; set; } = string.Empty;
        
        public bool Visible { get; set; } = true;
        public bool Selected { get; set; } = false;

        // Palette Color reference
        public string? PaletteColorId { get; set; }

        // --- Transformation Properties ---
        public double TranslateX { get; set; } = 0;
        public double TranslateY { get; set; } = 0;
        public double ScaleX { get; set; } = 1.0;
        public double ScaleY { get; set; } = 1.0;
        public double Rotation { get; set; } = 0; // In degrees
        public double SkewX { get; set; } = 0;
        public double SkewY { get; set; } = 0;
        public bool FlipX { get; set; } = false;
        public bool FlipY { get; set; } = false;
        
        // The Pivot point (Anchor) in local un-transformed coordinates
        // Automatically initialized to center of bounds on first selection if not set
        public Point? Pivot { get; set; } 

        public Stroke Clone()
        {
            return new Stroke
            {
                Type = this.Type,
                Points = this.Points.ToList(),
                Color = this.Color,
                Thickness = this.Thickness,
                Opacity = this.Opacity,
                FillColor = this.FillColor,
                Bounds = this.Bounds,
                Text = this.Text,
                Visible = this.Visible,
                Selected = this.Selected,
                PaletteColorId = this.PaletteColorId,
                TranslateX = this.TranslateX,
                TranslateY = this.TranslateY,
                ScaleX = this.ScaleX,
                ScaleY = this.ScaleY,
                Rotation = this.Rotation,
                SkewX = this.SkewX,
                SkewY = this.SkewY,
                FlipX = this.FlipX,
                FlipY = this.FlipY,
                Pivot = this.Pivot
            };
        }

        // --- Math & Geometry Helpers ---

        /// <summary>
        /// Gets the transformation matrix mapping local coordinates to screen coordinates.
        /// </summary>
        public Matrix GetRenderMatrix()
        {
            var p = GetPivot();
            var m = Matrix.CreateTranslation(-p.X, -p.Y); // Move to origin
            
            // Apply scale & flip
            double sx = ScaleX * (FlipX ? -1 : 1);
            double sy = ScaleY * (FlipY ? -1 : 1);
            m *= Matrix.CreateScale(sx, sy);
            
            // Apply skew
            if (SkewX != 0 || SkewY != 0)
                m *= Matrix.CreateSkew(MathUtilities.Deg2Rad(SkewX), MathUtilities.Deg2Rad(SkewY));
            
            // Apply rotation
            if (Rotation != 0)
                m *= Matrix.CreateRotation(MathUtilities.Deg2Rad(Rotation));
            
            // Move back and translate
            m *= Matrix.CreateTranslation(p.X + TranslateX, p.Y + TranslateY);
            
            return m;
        }

        /// <summary>
        /// Returns the exact un-transformed bounding box of the geometry.
        /// </summary>
        public Rect GetLocalBounds()
        {
            if (Type == StrokeType.Rectangle || Type == StrokeType.Ellipse || Type == StrokeType.Text)
                return Bounds;
            
            if (Points.Count == 0) return new Rect(0,0,0,0);
            
            double minX = Points.Min(p => p.X);
            double maxX = Points.Max(p => p.X);
            double minY = Points.Min(p => p.Y);
            double maxY = Points.Max(p => p.Y);
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Returns the screen-space bounding box after all transformations are applied.
        /// </summary>
        public Rect GetTransformedBounds()
        {
            var local = GetLocalBounds();
            var mat = GetRenderMatrix();
            var p1 = local.TopLeft.Transform(mat);
            var p2 = local.TopRight.Transform(mat);
            var p3 = local.BottomLeft.Transform(mat);
            var p4 = local.BottomRight.Transform(mat);

            double minX = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X));
            double maxX = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X));
            double minY = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));
            double maxY = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));
            
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public Point GetPivot()
        {
            if (Pivot.HasValue) return Pivot.Value;
            var b = GetLocalBounds();
            return new Point(b.Center.X, b.Center.Y);
        }

        /// <summary>
        /// Checks if a screen point intersects with this stroke, taking transforms into account.
        /// </summary>
        public bool HitTestPoint(Point screenPoint, double hitTolerance = 5.0)
        {
            var matrix = GetRenderMatrix();
            if (!matrix.HasInverse) return false; // Edge case (Scale = 0)
            
            var localPoint = screenPoint.Transform(matrix.Invert());
            
            if (Type == StrokeType.Rectangle || Type == StrokeType.Ellipse || Type == StrokeType.Text)
            {
                // Quick AABB check in local space
                if (FillColor != Colors.Transparent)
                    return Bounds.Contains(localPoint);
                else
                {
                    // If no fill, only hit the border
                    var inflated = Bounds.Inflate(hitTolerance + Thickness / 2);
                    var deflated = Bounds.Deflate(hitTolerance + Thickness / 2);
                    return inflated.Contains(localPoint) && !deflated.Contains(localPoint);
                }
            }
            
            // For Freehand and Line, check distance to line segments
            if (Points.Count < 2) return false;
            
            double threshSq = (hitTolerance + Thickness / 2) * (hitTolerance + Thickness / 2);
            for (int i = 0; i < Points.Count - 1; i++)
            {
                if (MathUtilities.DistanceSquaredPointToLineSegment(localPoint, Points[i], Points[i + 1]) <= threshSq)
                    return true;
            }
            return false;
        }
    }

    public static class MathUtilities
    {
        public static double Deg2Rad(double deg) => deg * Math.PI / 180.0;
        
        public static double DistanceSquaredPointToLineSegment(Point p, Point v, Point w)
        {
            double l2 = Dist2(v, w);
            if (l2 == 0) return Dist2(p, v);
            double t = ((p.X - v.X) * (w.X - v.X) + (p.Y - v.Y) * (w.Y - v.Y)) / l2;
            t = Math.Max(0, Math.Min(1, t));
            return Dist2(p, new Point(v.X + t * (w.X - v.X), v.Y + t * (w.Y - v.Y)));
        }
        
        private static double Dist2(Point v, Point w)
        {
            return (v.X - w.X) * (v.X - w.X) + (v.Y - w.Y) * (v.Y - w.Y);
        }
    }
}
