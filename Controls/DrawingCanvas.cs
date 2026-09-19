using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Ersm_Animation_App.Tools;
using Ersm_Animation_App.Models;

namespace Ersm_Animation_App
{
    public partial class DrawingCanvas : Control
    {
        public AnimationProject? Project { get; set; }
        
        private MainWindow.ToolType _currentToolType = MainWindow.ToolType.Pencil;
        public MainWindow.ToolType CurrentTools
        {
            get => _currentToolType;
            set
            {
                _currentToolType = value;
                UpdateActiveTool();
            }
        }
        
        public Color CurrentColor { get; set; } = Colors.Black;
        public Color FillColor { get; set; } = Colors.Transparent;
        public double CurrentThickness { get; set; } = 2.0;
        public double CurrentOpacity { get; set; } = 1.0;
        public int CurrentEraserMode { get; set; } = 0;
        public double CurrentEraserSize { get; set; } = 20.0;
        
        public string? ActivePaletteColorId { get; set; }

        public bool OnionSkinEnabled { get; set; } = false;
        public int OnionSkinPrevCount { get; set; } = 1;
        public int OnionSkinNextCount { get; set; } = 0;

        // Undo/Redo with capacity limit to prevent memory leak
        private const int MaxUndoSteps = 50;
        private Stack<List<Stroke>> _undoStack = new();
        private Stack<List<Stroke>> _redoStack = new();

        private Dictionary<MainWindow.ToolType, ITool> _tools = new();
        private ITool? _activeTool;

        public DrawingCanvas()
        {
            ClipToBounds = true;
            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
            PointerReleased += OnPointerReleased;

            _tools[MainWindow.ToolType.Pencil] = new PencilTool();
            _tools[MainWindow.ToolType.Brush] = new BrushTool();
            _tools[MainWindow.ToolType.Eraser] = new EraserTool();
            _tools[MainWindow.ToolType.Select] = new SelectTool();
            _tools[MainWindow.ToolType.Lasso] = new LassoTool();
            _tools[MainWindow.ToolType.Bucket] = new BucketTool();
            _tools[MainWindow.ToolType.Line] = new LineTool();
            _tools[MainWindow.ToolType.Rect] = new RectTool();
            _tools[MainWindow.ToolType.Ellipse] = new EllipseTool();
            _tools[MainWindow.ToolType.Text] = new TextTool();
            
            UpdateActiveTool();
        }

        private void UpdateActiveTool()
        {
            if (_tools.TryGetValue(CurrentTools, out var tool))
            {
                _activeTool = tool;
            }
        }

        public void SaveUndoState()
        {
            var currentFrame = GetCurrentFrame();
            if (currentFrame == null) return;

            var clonedStrokes = currentFrame.Strokes.Select(s => s.Clone()).ToList();
            _undoStack.Push(clonedStrokes);
            _redoStack.Clear();

            // Enforce capacity limit
            if (_undoStack.Count > MaxUndoSteps)
            {
                var temp = _undoStack.ToArray();
                _undoStack.Clear();
                for (int i = 0; i < MaxUndoSteps; i++)
                    _undoStack.Push(temp[MaxUndoSteps - 1 - i]);
            }
        }

        public void Undo()
        {
            if (_undoStack.Count > 0)
            {
                var currentFrame = GetCurrentFrame();
                if (currentFrame != null)
                {
                    _redoStack.Push(currentFrame.Strokes.Select(s => s.Clone()).ToList());
                    currentFrame.Strokes = _undoStack.Pop();
                    InvalidateVisual();
                }
            }
        }

        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                var currentFrame = GetCurrentFrame();
                if (currentFrame != null)
                {
                    _undoStack.Push(currentFrame.Strokes.Select(s => s.Clone()).ToList());
                    currentFrame.Strokes = _redoStack.Pop();
                    InvalidateVisual();
                }
            }
        }

        public void ClearCanvas()
        {
            SaveUndoState();
            var currentFrame = GetCurrentFrame();
            if (currentFrame != null)
            {
                currentFrame.Strokes.Clear();
                InvalidateVisual();
            }
        }

        public AnimationFrame? GetCurrentFrame()
        {
            if (Project == null || Project.Layers.Count == 0) return null;
            
            // Use ActiveLayerId if set, otherwise fallback to first visible unlocked layer
            var activeLayer = Project.Layers.FirstOrDefault(l => l.Id == Project.ActiveLayerId);
            if (activeLayer == null || !activeLayer.Visible || activeLayer.IsLocked)
            {
                activeLayer = Project.Layers.FirstOrDefault(l => l.Visible && !l.IsLocked);
            }
            
            if (activeLayer == null) return null;

            var frame = activeLayer.Frames.FirstOrDefault(f => f.Index == Project.CurrentFrameIndex);
            if (frame == null)
            {
                frame = new AnimationFrame { Index = Project.CurrentFrameIndex };
                activeLayer.Frames.Add(frame);
            }
            return frame;
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var currentFrame = GetCurrentFrame();
            if (currentFrame != null && _activeTool != null)
            {
                _activeTool.OnPointerPressed(this, e, currentFrame);
            }
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            var currentFrame = GetCurrentFrame();
            if (currentFrame != null && _activeTool != null)
            {
                _activeTool.OnPointerMoved(this, e, currentFrame);
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            var currentFrame = GetCurrentFrame();
            if (currentFrame != null && _activeTool != null)
            {
                _activeTool.OnPointerReleased(this, e, currentFrame);
            }
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (Project == null) return;

            // Draw Background â€” use this control's context to find resources
            var bgBrush = (IBrush?)(this.FindResource("CanvasBg") ?? Brushes.White);
            context.FillRectangle(bgBrush ?? Brushes.White, new Rect(Bounds.Size));

            // Onion Skinning
            if (OnionSkinEnabled)
            {
                for (int i = 1; i <= OnionSkinPrevCount; i++)
                {
                    DrawFrameStrokes(context, Project.CurrentFrameIndex - i, 0.3, Colors.Red);
                }
                for (int i = 1; i <= OnionSkinNextCount; i++)
                {
                    DrawFrameStrokes(context, Project.CurrentFrameIndex + i, 0.3, Colors.Green);
                }
            }

            DrawFrameStrokes(context, Project.CurrentFrameIndex, 1.0, null);

            _activeTool?.Render(context, this);
        }

        public void DeleteSelectedStrokes()
        {
            if (Project == null) return;
            SaveUndoState();
            var frame = Project.Layers.SelectMany(l => l.Frames).FirstOrDefault(f => f.Index == Project.CurrentFrameIndex);
            if (frame != null)
            {
                frame.Strokes.RemoveAll(s => s.Selected);
                InvalidateVisual();
            }
        }

        private void DrawFrameStrokes(DrawingContext context, int frameIndex, double opacityMultiplier, Color? overrideColor)
        {
            if (frameIndex < 0 || Project == null) return;

            foreach (var layer in Project.Layers)
            {
                if (!layer.Visible) continue;

                var frame = layer.Frames.FirstOrDefault(f => f.Index == frameIndex);
                if (frame == null) continue;

                foreach (var stroke in frame.Strokes)
                {
                    if (!stroke.Visible) continue;

                    var actualColor = Project.Palette.ResolveStrokeColor(stroke);
                    var strokeColor = overrideColor ?? actualColor;
                    var strokeBrush = new SolidColorBrush(strokeColor, stroke.Opacity * opacityMultiplier * layer.Opacity);
                    var pen = new Pen(strokeBrush, stroke.Thickness);
                    
                    var actualFillColor = Project.Palette.ResolveFillColor(stroke);
                    var fillBrush = actualFillColor == Colors.Transparent ? null : new SolidColorBrush(actualFillColor, stroke.Opacity * opacityMultiplier * layer.Opacity);

                    using (context.PushTransform(stroke.GetRenderMatrix()))
                    {
                        if (stroke.Type == StrokeType.Freehand)
                        {
                            if (stroke.Points.Count < 2) continue;
                            var geometry = new StreamGeometry();
                            using (var ctx = geometry.Open())
                            {
                                ctx.BeginFigure(stroke.Points[0], false);
                                for (int i = 1; i < stroke.Points.Count; i++)
                                {
                                    ctx.LineTo(stroke.Points[i]);
                                }
                            }
                            var smoothPen = new Pen(strokeBrush, stroke.Thickness, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
                            context.DrawGeometry(null, smoothPen, geometry);
                        }
                        else if (stroke.Type == StrokeType.Line)
                        {
                            if (stroke.Points.Count < 2) continue;
                            var linePen = new Pen(strokeBrush, stroke.Thickness, lineCap: PenLineCap.Round);
                            context.DrawLine(linePen, stroke.Points[0], stroke.Points[^1]);
                        }
                        else if (stroke.Type == StrokeType.Rectangle)
                        {
                            context.DrawRectangle(fillBrush, pen, stroke.Bounds);
                        }
                        else if (stroke.Type == StrokeType.Ellipse)
                        {
                            context.DrawEllipse(fillBrush, pen, stroke.Bounds.Center, stroke.Bounds.Width / 2, stroke.Bounds.Height / 2);
                        }
                        else if (stroke.Type == StrokeType.Text)
                        {
                            if (!string.IsNullOrEmpty(stroke.Text))
                            {
                                var textBrush = new SolidColorBrush(strokeColor, stroke.Opacity * opacityMultiplier * layer.Opacity);
                                var formattedText = new FormattedText(
                                    stroke.Text,
                                    System.Globalization.CultureInfo.CurrentCulture,
                                    FlowDirection.LeftToRight,
                                    new Typeface("Arial"),
                                    stroke.Thickness > 0 ? stroke.Thickness : 24,
                                    textBrush);
                                context.DrawText(formattedText, stroke.Bounds.TopLeft);
                            }
                        }
                    }
                }
            }
        }
    }
}