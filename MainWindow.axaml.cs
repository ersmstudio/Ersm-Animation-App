using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Ersm_Animation_App.Models;
using Ersm_Animation_App.Services;
using Avalonia.Platform.Storage;
using System;
using System.Linq;

namespace Ersm_Animation_App
{
    public partial class MainWindow : Window
    {
        public enum ToolType { Select, Lasso, Pencil, Brush, Bucket, Eraser, Line, Rect, Ellipse, Text }

        private AnimationProject _project;
        private string? _currentFilePath;
        private DispatcherTimer _playTimer;
        private bool _isPlaying = false;
        private bool _isDirty = false;

        public MainWindow(AnimationProject project, string? filePath = null)
        {
            InitializeComponent();
            _project = project;
            _currentFilePath = filePath;

            // Setup Project properties in UI
            AnimCanvas.Project = _project;
            AnimCanvas.Width = _project.CanvasWidth;
            AnimCanvas.Height = _project.CanvasHeight;
            RightPanel.BindProject(_project);
            Timeline.BindProject(_project);
            
            // Fit canvas on load
            Loaded += (s, e) => ZoomCtrl.FitToScreen();

            // Connect Tool Settings events
            RightPanel.ProjWidth.ValueChanged += (s, e) => { _project.CanvasWidth = (int)(RightPanel.ProjWidth.Value ?? 1920); AnimCanvas.Width = _project.CanvasWidth; SetDirty(); };
            RightPanel.ProjHeight.ValueChanged += (s, e) => { _project.CanvasHeight = (int)(RightPanel.ProjHeight.Value ?? 1080); AnimCanvas.Height = _project.CanvasHeight; SetDirty(); };
            RightPanel.ProjFPS.ValueChanged += (s, e) => { _project.FPS = (int)(RightPanel.ProjFPS.Value ?? 24); UpdateTimerInterval(); SetDirty(); };
            RightPanel.ProjLoop.IsCheckedChanged += (s, e) => { _project.IsLooping = RightPanel.ProjLoop.IsChecked ?? false; SetDirty(); };
            
            ToolSettings.ThicknessSlider.ValueChanged += (s, e) => { AnimCanvas.CurrentThickness = ToolSettings.GetThickness(); };
            ToolSettings.OpacitySlider.ValueChanged += (s, e) => { AnimCanvas.CurrentOpacity = ToolSettings.GetOpacity(); };
            ToolSettings.EraserModeCombo.SelectionChanged += (s, e) => { AnimCanvas.CurrentEraserMode = ToolSettings.GetEraserMode(); };
            ToolSettings.EraserSizeSlider.ValueChanged += (s, e) => { AnimCanvas.CurrentEraserSize = ToolSettings.GetEraserSize(); };
            
            RightPanel.PaletteCtrl.BindPalette(_project.Palette);
            
            RightPanel.PaletteCtrl.PaletteColorSelected += (pc) => {
                AnimCanvas.ActivePaletteColorId = pc.Id;
                AnimCanvas.CurrentColor = pc.Color;
                StrokeColorBorder.Background = new SolidColorBrush(pc.Color);
            };
            
            RightPanel.PaletteCtrl.PaletteColorChanged += (pc) => {
                // If it's the active color, update the UI pickers
                if (AnimCanvas.ActivePaletteColorId == pc.Id) {
                    AnimCanvas.CurrentColor = pc.Color;
                    StrokeColorBorder.Background = new SolidColorBrush(pc.Color);
                }
                // Redraw canvas so all strokes using this PaletteColorId update
                AnimCanvas.InvalidateVisual();
                SetDirty();
            };

            RightPanel.PaletteCtrl.PaletteColorDeleted += (pc) => {
                // Remove from all frames/layers
                ColorPalette.DeleteColorFromProject(_project, pc.Id);
                AnimCanvas.InvalidateVisual();
                SetDirty();
            };

            // Connect Onion Skin
            RightPanel.OnionSkinEnabled.IsCheckedChanged += (s, e) => { AnimCanvas.OnionSkinEnabled = RightPanel.OnionSkinEnabled.IsChecked ?? false; AnimCanvas.InvalidateVisual(); };
            RightPanel.OnionSkinPrev.ValueChanged += (s, e) => { AnimCanvas.OnionSkinPrevCount = (int)RightPanel.OnionSkinPrev.Value; AnimCanvas.InvalidateVisual(); };
            RightPanel.OnionSkinNext.ValueChanged += (s, e) => { AnimCanvas.OnionSkinNextCount = (int)RightPanel.OnionSkinNext.Value; AnimCanvas.InvalidateVisual(); };

            // Tool Settings
            ToolSettings.ThicknessSlider.ValueChanged += (s, e) => AnimCanvas.CurrentThickness = ToolSettings.GetThickness();
            ToolSettings.OpacitySlider.ValueChanged += (s, e) => AnimCanvas.CurrentOpacity = ToolSettings.GetOpacity();
            
            // Dual Colors
            StrokeColorPicker.ColorChanged += color =>
            {
                StrokeColorBorder.Background = new SolidColorBrush(color);
                AnimCanvas.CurrentColor = color;
            };
            StrokeColorPicker.CloseRequested += () => StrokeColorBtn.Flyout?.Hide();

            FillColorPicker.ColorChanged += color =>
            {
                FillColorBorder.Background = new SolidColorBrush(color);
                AnimCanvas.FillColor = color;
            };
            FillColorPicker.CloseRequested += () => FillColorBtn.Flyout?.Hide();

            // Select Panel events
            ToolSettings.DeleteRequested += (s, e) => { AnimCanvas.DeleteSelectedStrokes(); SetDirty(); };
            ToolSettings.FlipHRequested += (s, e) => {
                foreach (var st in _project.Layers.SelectMany(l => l.Frames.Where(f => f.Index == _project.CurrentFrameIndex).SelectMany(f => f.Strokes)))
                    if (st.Selected) st.FlipX = !st.FlipX;
                AnimCanvas.InvalidateVisual();
                SetDirty();
            };
            ToolSettings.FlipVRequested += (s, e) => {
                foreach (var st in _project.Layers.SelectMany(l => l.Frames.Where(f => f.Index == _project.CurrentFrameIndex).SelectMany(f => f.Strokes)))
                    if (st.Selected) st.FlipY = !st.FlipY;
                AnimCanvas.InvalidateVisual();
                SetDirty();
            };

            // Timeline Events
            Timeline.FrameSelected += (index) => { _project.CurrentFrameIndex = index; UpdateStatus(); AnimCanvas.InvalidateVisual(); Timeline.RefreshTimeline(); };
            Timeline.PlayRequested += PlayAnimation;
            Timeline.PauseRequested += PauseAnimation;
            Timeline.StopRequested += StopAnimation;
            Timeline.FrameAdded += () => {
                var layer = _project.Layers.First();
                if (layer.Frames.Count >= 1000) return; // Safety limit to prevent memory crash
                layer.Frames.Add(new AnimationFrame { Index = layer.Frames.Count });
                _project.CurrentFrameIndex = layer.Frames.Count - 1;
                Timeline.RefreshTimeline();
                AnimCanvas.InvalidateVisual();
                SetDirty();
                UpdateStatus();
            };

            // Playback Timer
            _playTimer = new DispatcherTimer();
            _playTimer.Tick += PlayTimer_Tick;
            UpdateTimerInterval();
            UpdateStatus();
            
            // Mark canvas dirty when drawn on
            AnimCanvas.PointerReleased += (s, e) => SetDirty();
        }

        public MainWindow() : this(new AnimationProject()) { }

        private void SetDirty() { _isDirty = true; StatusSavedText.Text = "Unsaved changes"; }

        private void UpdateTimerInterval() { if (_project.FPS > 0) _playTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / _project.FPS); }

        private void PlayAnimation() { if (_isPlaying) return; _isPlaying = true; _playTimer.Start(); }
        private void PauseAnimation() { _isPlaying = false; _playTimer.Stop(); }
        private void StopAnimation() { PauseAnimation(); _project.CurrentFrameIndex = 0; Timeline.RefreshTimeline(); AnimCanvas.InvalidateVisual(); UpdateStatus(); }

        private void PlayTimer_Tick(object? sender, EventArgs e)
        {
            int maxFrames = _project.Layers.Max(l => l.Frames.Count);
            if (maxFrames == 0) return;

            _project.CurrentFrameIndex++;
            if (_project.CurrentFrameIndex >= maxFrames)
            {
                if (_project.IsLooping) _project.CurrentFrameIndex = _project.LoopStartFrame;
                else { StopAnimation(); return; }
            }
            Timeline.RefreshTimeline();
            AnimCanvas.InvalidateVisual();
            UpdateStatus();
        }

        private void MenuClearFrame_Click(object? sender, RoutedEventArgs e)
        {
            // If something is selected, just delete that. Otherwise clear the whole frame.
            if (AnimCanvas.CurrentTools == ToolType.Select)
            {
                AnimCanvas.DeleteSelectedStrokes();
                SetDirty();
                return;
            }

            if (_project == null) return;
            AnimCanvas.SaveUndoState();
            foreach (var layer in _project.Layers)
            {
                if (!layer.Visible || layer.IsLocked) continue;
                var frame = layer.Frames.FirstOrDefault(f => f.Index == _project.CurrentFrameIndex);
                if (frame != null)
                {
                    frame.Strokes.Clear();
                }
            }
            AnimCanvas.InvalidateVisual();
            SetDirty();
        }

        private void UpdateStatus()
        {
            int maxFrames = Math.Max(1, _project.Layers.Count > 0 ? _project.Layers.Max(l => l.Frames.Count) : 1);
            StatusFrameText.Text = $"Frame: {_project.CurrentFrameIndex + 1} / {maxFrames}";
            StatusToolText.Text = $"Tool: {AnimCanvas.CurrentTools}";
        }

        // TOOL BUTTONS
        private void Tool_Click(object? sender, RoutedEventArgs e)
        {
            BtnSelect.IsChecked = BtnLasso.IsChecked = false;
            BtnPencil.IsChecked = BtnBrush.IsChecked = BtnBucket.IsChecked = BtnEraser.IsChecked = false;
            BtnLine.IsChecked = BtnRect.IsChecked = BtnEllipse.IsChecked = BtnText.IsChecked = false;

            if (sender is ToggleButton btn) btn.IsChecked = true;

            if (sender == BtnSelect) { AnimCanvas.CurrentTools = ToolType.Select; ToolSettings.SetActiveTool("Select"); }
            if (sender == BtnLasso) { AnimCanvas.CurrentTools = ToolType.Lasso; ToolSettings.SetActiveTool("Lasso Select"); }
            if (sender == BtnPencil) { AnimCanvas.CurrentTools = ToolType.Pencil; ToolSettings.SetActiveTool("Pencil"); }
            if (sender == BtnBrush) { AnimCanvas.CurrentTools = ToolType.Brush; ToolSettings.SetActiveTool("Brush"); }
            if (sender == BtnBucket) { AnimCanvas.CurrentTools = ToolType.Bucket; ToolSettings.SetActiveTool("Paint Bucket"); }
            if (sender == BtnEraser) { AnimCanvas.CurrentTools = ToolType.Eraser; ToolSettings.SetActiveTool("Eraser"); }
            if (sender == BtnLine) { AnimCanvas.CurrentTools = ToolType.Line; ToolSettings.SetActiveTool("Line"); }
            if (sender == BtnRect) { AnimCanvas.CurrentTools = ToolType.Rect; ToolSettings.SetActiveTool("Rectangle"); }
            if (sender == BtnEllipse) { AnimCanvas.CurrentTools = ToolType.Ellipse; ToolSettings.SetActiveTool("Ellipse"); }
            if (sender == BtnText) { AnimCanvas.CurrentTools = ToolType.Text; ToolSettings.SetActiveTool("Text"); }

            UpdateStatus();
        }

        private void StrokeColorBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (StrokeColorBtn.Flyout is Flyout flyout)
            {
                StrokeColorPicker.BeginEdit(AnimCanvas.CurrentColor);
                flyout.ShowAt(StrokeColorBtn);
            }
        }

        private void FillColorBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (FillColorBtn.Flyout is Flyout flyout)
            {
                FillColorPicker.BeginEdit(AnimCanvas.FillColor);
                flyout.ShowAt(FillColorBtn);
            }
        }

        private void SwapColors_Click(object? sender, RoutedEventArgs e)
        {
            var temp = AnimCanvas.CurrentColor;
            AnimCanvas.CurrentColor = AnimCanvas.FillColor;
            AnimCanvas.FillColor = temp;
            
            StrokeColorBorder.Background = new SolidColorBrush(AnimCanvas.CurrentColor);
            FillColorBorder.Background = new SolidColorBrush(AnimCanvas.FillColor);
        }

        // MENU BAR HANDLERS
        private async void MenuNewProject_Click(object? sender, RoutedEventArgs e)
        {
            var welcome = new WelcomeWindow();
            welcome.Show();
            this.Close();
        }

        private async void MenuOpen_Click(object? sender, RoutedEventArgs e)
        {
            var options = new FilePickerOpenOptions { FileTypeFilter = new[] { new FilePickerFileType("Ersm") { Patterns = new[] { "*.ersma" } } } };
            var result = await StorageProvider.OpenFilePickerAsync(options);
            if (result.Count > 0)
            {
                var p = await ProjectFileService.LoadProjectAsync(result[0].Path.LocalPath);
                if (p != null)
                {
                    var mw = new MainWindow(p, result[0].Path.LocalPath);
                    mw.Show();
                    this.Close();
                }
            }
        }

        private async void MenuSave_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
                MenuSaveAs_Click(sender, e);
            else
                await SaveProject(_currentFilePath);
        }

        private async void MenuSaveAs_Click(object? sender, RoutedEventArgs e)
        {
            var options = new FilePickerSaveOptions { DefaultExtension = "ersma", FileTypeChoices = new[] { new FilePickerFileType("Ersm") { Patterns = new[] { "*.ersma" } } } };
            var file = await StorageProvider.SaveFilePickerAsync(options);
            if (file != null)
            {
                _currentFilePath = file.Path.LocalPath;
                await SaveProject(_currentFilePath);
            }
        }

        private async System.Threading.Tasks.Task SaveProject(string path)
        {
            RightPanel.UpdateProject(_project); // sync settings
            await ProjectFileService.SaveProjectAsync(_project, path);
            _isDirty = false;
            StatusSavedText.Text = "Saved successfully";
        }

        private void MenuExit_Click(object? sender, RoutedEventArgs e) => Close();

        private async void MenuExportMp4_Click(object? sender, RoutedEventArgs e)
        {
            var options = new Avalonia.Platform.Storage.FilePickerSaveOptions { DefaultExtension = "mp4", FileTypeChoices = new[] { new Avalonia.Platform.Storage.FilePickerFileType("MP4 Video") { Patterns = new[] { "*.mp4" } } } };
            var file = await StorageProvider.SaveFilePickerAsync(options);
            if (file != null)
            {
                StatusSavedText.Text = "Exporting MP4...";
                try
                {
                    await Services.ExportService.ExportAnimationAsync(AnimCanvas, _project, file.Path.LocalPath, isGif: false);
                    StatusSavedText.Text = "Export Complete.";
                }
                catch (System.Exception ex)
                {
                    StatusSavedText.Text = "Export Failed (Is FFmpeg installed?)";
                }
            }
        }

        private async void MenuExportGif_Click(object? sender, RoutedEventArgs e)
        {
            var options = new Avalonia.Platform.Storage.FilePickerSaveOptions { DefaultExtension = "gif", FileTypeChoices = new[] { new Avalonia.Platform.Storage.FilePickerFileType("GIF Animation") { Patterns = new[] { "*.gif" } } } };
            var file = await StorageProvider.SaveFilePickerAsync(options);
            if (file != null)
            {
                StatusSavedText.Text = "Exporting GIF...";
                try
                {
                    await Services.ExportService.ExportAnimationAsync(AnimCanvas, _project, file.Path.LocalPath, isGif: true);
                    StatusSavedText.Text = "Export Complete.";
                }
                catch (System.Exception ex)
                {
                    StatusSavedText.Text = "Export Failed (Is FFmpeg installed?)";
                }
            }
        }
        private async void Window_Closing(object? sender, WindowClosingEventArgs e)
        {
            if (_isDirty)
            {
                e.Cancel = true; // Cancel default close
                _isDirty = false; // Prevent infinite loop

                var result = await SaveConfirmDialog.ShowDialog(this);
                if (result == SaveConfirmResult.Save)
                {
                    if (string.IsNullOrEmpty(_currentFilePath))
                    {
                        var options = new FilePickerSaveOptions { DefaultExtension = "ersma", FileTypeChoices = new[] { new FilePickerFileType("Ersm") { Patterns = new[] { "*.ersma" } } } };
                        var file = await StorageProvider.SaveFilePickerAsync(options);
                        if (file != null)
                        {
                            await SaveProject(file.Path.LocalPath);
                            Close();
                        }
                        else
                        {
                            _isDirty = true; // User cancelled save dialog
                        }
                    }
                    else
                    {
                        await SaveProject(_currentFilePath);
                        Close();
                    }
                }
                else if (result == SaveConfirmResult.DontSave)
                {
                    Close(); // Close without saving
                }
                else
                {
                    _isDirty = true; // User cancelled closing
                }
            }
        }

        private void MenuUndo_Click(object? sender, RoutedEventArgs e) => AnimCanvas.Undo();
        private void MenuRedo_Click(object? sender, RoutedEventArgs e) => AnimCanvas.Redo();
        private void MenuPlayPause_Click(object? sender, RoutedEventArgs e) { if (_isPlaying) PauseAnimation(); else PlayAnimation(); }
        private void MenuAddFrame_Click(object? sender, RoutedEventArgs e) 
        {
            if (_project.Layers.First().Frames.Count >= 1000) return; // Frame Limit
            Timeline.BtnAddFrame.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); 
        }
        
        private void MenuAbout_Click(object? sender, RoutedEventArgs e)
        {
            // Simple about
        }
    }
}