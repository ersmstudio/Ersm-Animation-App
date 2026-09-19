using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Input;
using Ersm_Animation_App.Models;
using System;
using System.Linq;

namespace Ersm_Animation_App.Controls
{
    public partial class TimelinePanel : UserControl
    {
        public event Action<int>? FrameSelected;
        public event Action? FrameAdded;
        public event Action? FrameRemoved;
        public event Action? FrameDuplicated;
        public event Action? PlayRequested;
        public event Action? PauseRequested;
        public event Action? StopRequested;
        public event Action<Guid>? LayerSelected;

        private AnimationProject? _project;
        private const int ExtraFramesToDraw = 10; // Draw extra empty frames for clicking

        public TimelinePanel()
        {
            InitializeComponent();
            
            BtnPrevFrame.Click += (s, e) => { if (_project != null && _project.CurrentFrameIndex > 0) FrameSelected?.Invoke(_project.CurrentFrameIndex - 1); };
            BtnNextFrame.Click += (s, e) => { if (_project != null) FrameSelected?.Invoke(_project.CurrentFrameIndex + 1); };
            
            BtnPlay.Click += (s, e) => PlayRequested?.Invoke();
            BtnPause.Click += (s, e) => PauseRequested?.Invoke();
            BtnStop.Click += (s, e) => StopRequested?.Invoke();
            
            BtnAddFrame.Click += (s, e) => FrameAdded?.Invoke();
            BtnRemoveFrame.Click += (s, e) => FrameRemoved?.Invoke();
            BtnDupFrame.Click += (s, e) => FrameDuplicated?.Invoke();

            BtnAddLayer.Click += (s, e) => {
                if (_project != null)
                {
                    var newLayer = new Layer { Name = $"Layer {_project.Layers.Count + 1}" };
                    // Add just one frame at the current playhead
                    newLayer.Frames.Add(new AnimationFrame { Index = _project.CurrentFrameIndex });
                    _project.Layers.Add(newLayer);
                    _project.ActiveLayerId = newLayer.Id;
                    RefreshTimeline();
                }
            };
        }

        public void BindProject(AnimationProject project)
        {
            _project = project;
            if (_project.Layers.Any() && _project.ActiveLayerId == Guid.Empty)
            {
                _project.ActiveLayerId = _project.Layers.First().Id;
            }
            RefreshTimeline();
        }

        public void RefreshTimeline()
        {
            if (_project == null) return;
            LayersContainer.Children.Clear();

            int maxFrames = GetMaxFrames();
            // Ensure we draw enough grid for current playhead + extra space
            int drawLimit = Math.Max(maxFrames, _project.CurrentFrameIndex + ExtraFramesToDraw);

            var selectedBrush = (IBrush?)(this.FindResource("Selected") ?? Brushes.LightGray);
            var accentBrush = (IBrush?)(this.FindResource("Accent") ?? Brushes.DodgerBlue);
            var activeLayerBg = new SolidColorBrush(Color.Parse("#30FFFFFF")); // light highlight for active layer

            foreach (var layer in _project.Layers.ToList())
            {
                var layerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
                if (layer.Id == _project.ActiveLayerId)
                {
                    layerRow.Background = activeLayerBg;
                }
                
                // Allow clicking the row to select layer
                layerRow.PointerPressed += (s, e) => {
                    _project.ActiveLayerId = layer.Id;
                    LayerSelected?.Invoke(layer.Id);
                    RefreshTimeline();
                };
                
                // Layer Header UI
                var layerHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, Width = 200, Background = Brushes.Transparent };
                
                var visBtn = new Avalonia.Controls.Primitives.ToggleButton { Content = "\U0001F441", Width = 30, IsChecked = layer.Visible, Padding = new Avalonia.Thickness(0) };
                visBtn.Click += (s, e) => { layer.Visible = visBtn.IsChecked ?? true; FrameSelected?.Invoke(_project.CurrentFrameIndex); };
                
                var lockBtn = new Avalonia.Controls.Primitives.ToggleButton { Content = "\U0001F512", Width = 30, IsChecked = layer.IsLocked, Padding = new Avalonia.Thickness(0) };
                lockBtn.Click += (s, e) => { layer.IsLocked = lockBtn.IsChecked ?? false; };

                var layerLabel = new TextBox 
                { 
                    Text = layer.Name, 
                    Width = 80, 
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = layer.Id == _project.ActiveLayerId ? FontWeight.Bold : FontWeight.Normal,
                    Background = Brushes.Transparent,
                    BorderThickness = new Avalonia.Thickness(0)
                };
                layerLabel.KeyUp += (s, e) => layer.Name = layerLabel.Text ?? "Layer";
                layerLabel.GotFocus += (s, e) => { _project.ActiveLayerId = layer.Id; RefreshTimeline(); };

                var delBtn = new Button { Content = "\u274C", Width = 30, Foreground = Brushes.Red, Padding = new Avalonia.Thickness(0) };
                delBtn.Click += (s, e) => { 
                    if (_project.Layers.Count <= 1) return; // Don't delete last layer
                    _project.Layers.Remove(layer); 
                    if (_project.ActiveLayerId == layer.Id) _project.ActiveLayerId = _project.Layers.First().Id;
                    RefreshTimeline(); 
                    FrameSelected?.Invoke(_project.CurrentFrameIndex); 
                };

                layerHeader.Children.Add(visBtn);
                layerHeader.Children.Add(lockBtn);
                layerHeader.Children.Add(layerLabel);
                layerHeader.Children.Add(delBtn);

                layerRow.Children.Add(layerHeader);

                for (int i = 0; i < drawLimit; i++)
                {
                    // Look for a frame in this layer at index i
                    var frame = layer.Frames.FirstOrDefault(f => f.Index == i);
                    
                    var frameBtn = new Button
                    {
                        Width = 30,
                        Height = 30,
                        Padding = new Avalonia.Thickness(0),
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center
                    };

                    // Draw dots instead of numbers
                    if (frame != null)
                    {
                        if (frame.Strokes.Count > 0)
                        {
                            // Filled Dot (Keyframe with strokes)
                            frameBtn.Content = new Ellipse { Width = 12, Height = 12, Fill = Brushes.DarkGray };
                        }
                        else
                        {
                            // Hollow Dot (Empty frame, but exists)
                            frameBtn.Content = new Ellipse { Width = 12, Height = 12, Stroke = Brushes.Gray, StrokeThickness = 2 };
                        }
                    }
                    else
                    {
                        // No frame exists here, leave it empty.
                        frameBtn.Content = null;
                    }

                    var fIndex = i;
                    frameBtn.Click += (s, e) => {
                        _project.ActiveLayerId = layer.Id;
                        FrameSelected?.Invoke(fIndex);
                    };

                    if (i == _project.CurrentFrameIndex)
                    {
                        frameBtn.Background = selectedBrush;
                        frameBtn.BorderBrush = accentBrush;
                        frameBtn.BorderThickness = new Avalonia.Thickness(2);
                    }

                    layerRow.Children.Add(frameBtn);
                }

                LayersContainer.Children.Add(layerRow);
            }
        }

        private int GetMaxFrames()
        {
            if (_project == null || _project.Layers.Count == 0) return 0;
            return _project.Layers.Max(l => l.Frames.Count > 0 ? l.Frames.Max(f => f.Index) + 1 : 1);
        }
    }
}