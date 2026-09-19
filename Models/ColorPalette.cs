using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace Ersm_Animation_App.Models
{
    /// <summary>
    /// A single named color in the palette. All strokes referencing this PaletteColor's Id
    /// will automatically update when the color changes (Toon Boom Harmony style).
    /// </summary>
    public class PaletteColor
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string Name { get; set; } = "Color";
        public Color Color { get; set; } = Colors.Black;
    }

    /// <summary>
    /// A collection of PaletteColors that belongs to a project.
    /// When a PaletteColor is changed, all strokes using that color update.
    /// When a PaletteColor is deleted, all strokes using it are removed.
    /// </summary>
    public class ColorPalette
    {
        public string Name { get; set; } = "Default Palette";
        public List<PaletteColor> Colors { get; set; } = new();

        public PaletteColor AddColor(string name, Color color)
        {
            var pc = new PaletteColor { Name = name, Color = color };
            Colors.Add(pc);
            return pc;
        }

        public PaletteColor? FindById(string? id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return Colors.FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// Update a palette color. All strokes referencing this ID will visually update
        /// on next render because the renderer resolves colors via the palette.
        /// </summary>
        public void UpdateColor(string id, Color newColor)
        {
            var pc = FindById(id);
            if (pc != null) pc.Color = newColor;
        }

        /// <summary>
        /// Delete a palette color and all strokes drawn with it across all layers/frames.
        /// </summary>
        public static void DeleteColorFromProject(AnimationProject project, string paletteColorId)
        {
            // Remove palette entry
            project.Palette.Colors.RemoveAll(c => c.Id == paletteColorId);

            // Remove all strokes using this color across every layer and frame
            foreach (var layer in project.Layers)
            {
                foreach (var frame in layer.Frames)
                {
                    frame.Strokes.RemoveAll(s => s.PaletteColorId == paletteColorId);
                }
            }
        }

        /// <summary>
        /// Resolve the actual Color for a stroke. If the stroke has a PaletteColorId,
        /// look up the current palette color. Otherwise fall back to the stroke's own Color.
        /// </summary>
        public Color ResolveStrokeColor(Stroke stroke)
        {
            if (!string.IsNullOrEmpty(stroke.PaletteColorId))
            {
                var pc = FindById(stroke.PaletteColorId);
                if (pc != null) return pc.Color;
            }
            return stroke.Color;
        }

        public Color ResolveFillColor(Stroke stroke)
        {
            // For now fill uses the stroke's own FillColor
            // Could be extended to have a separate palette fill reference
            return stroke.FillColor;
        }

        /// <summary>
        /// Create a default palette with basic animation colors.
        /// </summary>
        public static ColorPalette CreateDefault()
        {
            var p = new ColorPalette();
            p.AddColor("Black", Avalonia.Media.Colors.Black);
            p.AddColor("White", Avalonia.Media.Colors.White);
            p.AddColor("Red", Color.FromRgb(220, 50, 50));
            p.AddColor("Blue", Color.FromRgb(50, 100, 220));
            p.AddColor("Green", Color.FromRgb(50, 180, 80));
            p.AddColor("Yellow", Color.FromRgb(240, 220, 50));
            p.AddColor("Skin", Color.FromRgb(245, 210, 180));
            p.AddColor("Brown", Color.FromRgb(139, 90, 43));
            return p;
        }
    }
}
