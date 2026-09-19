using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
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

        private AnimationProject? _project;

        public TimelinePanel()
        {
            InitializeComponent();
            
            BtnPrevFrame.Click += (s, e) => { if (_project != null && _project.CurrentFrameIndex > 0) FrameSelected?.Invoke(_project.CurrentFrameIndex - 1); };
            BtnNextFrame.Click += (s, e) => { if (_project != null && _project.CurrentFrameIndex < GetMaxFrames() - 1) FrameSelected?.Invoke(_project.CurrentFrameIndex + 1); };
            
            BtnPlay.Click += (s, e) => PlayRequested?.Invoke();
            BtnPause.Click += (s, e) => PauseRequested?.Invoke();
            BtnStop.Click += (s, e) => StopRequested?.Invoke();
            
            BtnAddFrame.Click += (s, e) => FrameAdded?.Invoke();
            BtnRemoveFrame.Click += (s, e) => FrameRemoved?.Invoke();
            BtnDupFrame.Click += (s, e) => FrameDuplicated?.Invoke();

            BtnAddLayer.Click += (s, e) => {
                if (_project != null)
                {
                    _project.Layers.Add(new Layer { Name = $"Layer {_project.Layers.Count + 1}" });
                    RefreshTimeline();
                }
            };
        }

        public void BindProject(AnimationProject project)
        {
            _project = project;
            RefreshTimeline();
        }

        public void RefreshTimeline()
        {
            if (_project == null) return;
            LayersContainer.Children.Clear();

            int maxFrames = GetMaxFrames();
            if (maxFrames == 0) maxFrames = 1;

            foreach (var layer in _project.Layers.ToList()) // ToList to avoid modification exceptions if we delete
            {
                var layerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
                
                // Layer Header UI
                var layerHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, Width = 200, Background = Brushes.Transparent };
                
                var visBtn = new Avalonia.Controls.Primitives.ToggleButton { Content = "👁", Width = 30, IsChecked = layer.Visible, Padding=new Avalonia.Thickness(0) };
                visBtn.Click += (s, e) => { layer.Visible = visBtn.IsChecked ?? true; FrameSelected?.Invoke(_project.CurrentFrameIndex); };
                
                var lockBtn = new Avalonia.Controls.Primitives.ToggleButton { Content = "🔒", Width = 30, IsChecked = layer.Locked, Padding=new Avalonia.Thickness(0) };
                lockBtn.Click += (s, e) => { layer.Locked = lockBtn.IsChecked ?? false; };

                var layerLabel = new TextBox 
                { 
                    Text = layer.Name, 
                    Width = 80, 
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeight.Bold,
                    Background = Brushes.Transparent,
                    BorderThickness = new Avalonia.Thickness(0)
                };
                layerLabel.KeyUp += (s, e) => layer.Name = layerLabel.Text ?? "Layer";

                var delBtn = new Button { Content = "❌", Width = 30, Foreground = Brushes.Red, Padding=new Avalonia.Thickness(0) };
                delBtn.Click += (s, e) => { _project.Layers.Remove(layer); RefreshTimeline(); FrameSelected?.Invoke(_project.CurrentFrameIndex); };

                layerHeader.Children.Add(visBtn);
                layerHeader.Children.Add(lockBtn);
                layerHeader.Children.Add(layerLabel);
                layerHeader.Children.Add(delBtn);

                layerRow.Children.Add(layerHeader);

                for (int i = 0; i < maxFrames; i++)
                {
                    var frameBtn = new Button
                    {
                        Content = (i + 1).ToString(),
                        Width = 30,
                        Height = 30,
                        Padding = new Avalonia.Thickness(0),
                        HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
                    };

                    var fIndex = i; // capture
                    frameBtn.Click += (s, e) => FrameSelected?.Invoke(fIndex);

                    if (i == _project.CurrentFrameIndex)
                    {
                        frameBtn.Background = (IBrush)App.Current.FindResource("Selected");
                        frameBtn.BorderBrush = (IBrush)App.Current.FindResource("Accent");
                        frameBtn.BorderThickness = new Avalonia.Thickness(2);
                    }

                    // Check if frame has content
                    var frame = layer.Frames.FirstOrDefault(f => f.Index == i);
                    if (frame != null && frame.Strokes.Count > 0)
                    {
                        frameBtn.Foreground = Brushes.DarkGreen;
                        frameBtn.FontWeight = FontWeight.Bold;
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
