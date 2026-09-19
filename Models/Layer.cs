using System;
using System.Collections.Generic;

namespace Ersm_Animation_App.Models
{
    public class Layer
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = "Layer";
        public bool Visible { get; set; } = true;
        public bool IsLocked { get; set; } = false;
        /// <summary>Alias for IsLocked for backward compatibility</summary>
        public bool Locked { get => IsLocked; set => IsLocked = value; }
        public double Opacity { get; set; } = 1.0;
        public LayerType Type { get; set; } = LayerType.Vector;

        public List<AnimationFrame> Frames { get; set; } = new();
    }
}