using System;
using System.Collections.Generic;

namespace Ersm_Animation_App.Models
{
    public class AnimationFrame
    {
        public Guid Id { get; } = Guid.NewGuid();
        public int Index { get; set; } = 0;
        public FrameType Type { get; set; } = FrameType.Drawing;
        public int Duration { get; set; } = 1;

        public List<Stroke> Strokes { get; set; } = new();

        public AnimationFrame Clone()
        {
            var clone = new AnimationFrame
            {
                Index = this.Index,
                Type = this.Type,
                Duration = this.Duration,
            };

            foreach (var stroke in Strokes)
            {
                clone.Strokes.Add(stroke.Clone());
            }

            return clone;
        }
    }
}
