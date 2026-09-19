using System;
using System.Collections.Generic;

namespace Ersm_Animation_App.Models
{
    public class AnimationProject
    {
        public string ProjectName { get; set; } = "Untitled";
        public int CanvasWidth { get; set; } = 1920;
        public int CanvasHeight { get; set; } = 1080;
        public int FPS { get; set; } = 24;
        
        public bool IsLooping { get; set; } = false;
        public int LoopStartFrame { get; set; } = 0;
        public int LoopEndFrame { get; set; } = 0;

        public List<Layer> Layers { get; set; } = new();
        public List<string> Swatches { get; set; } = new();
        
        /// <summary>
        /// Toon Boom Harmony-style color palette. 
        /// All strokes reference colors from this palette by ID.
        /// </summary>
        public ColorPalette Palette { get; set; } = ColorPalette.CreateDefault();

        public int CurrentFrameIndex { get; set; } = 0;
    }
}
