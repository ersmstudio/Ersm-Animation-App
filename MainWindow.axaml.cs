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
#if DEBUG
            this.AttachDevTools();
#endif
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
            
            // Tool Settings (single subscription only)
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
                if (AnimCanvas.ActivePaletteColorId == pc.Id) {
                    AnimCanvas.CurrentColor = pc.Color;
                    StrokeColorBorder.Background = new SolidColorBrush(pc.Color);
                }
                AnimCanvas.InvalidateVisual();
                SetDirty();
            };

            RightPanel.PaletteCtrl.PaletteColorDeleted += (pc) => {
                ColorPalette.DeleteColorFromProject(_project, pc.Id);
                AnimCanvas.InvalidateVisual();
                SetDirty();
            };

            // Connect Onion Skin
            RightPanel.OnionSkinEnabled.IsCheckedChanged += (s, e) => { AnimCanvas.OnionSkinEnabled = RightPanel.OnionSkinEnabled.IsChecked ?? false; AnimCanvas.InvalidateVisual(); };
            RightPanel.OnionSkinPrev.ValueChanged += (s, e) => { AnimCanvas.OnionSkinPrevCount = (int)RightPanel.OnionSkinPrev.Value; AnimCanvas.InvalidateVisual(); };
            RightPanel.OnionSkinNext.ValueChanged += (s, e) => { AnimCanvas.OnionSkinNextCount = (int)RightPanel.OnionSkinNext.Value; AnimCanvas.InvalidateVisual(); };

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
                AnimCanvas.SaveUndoState();
                foreach (var st in _project.Layers.SelectMany(l => l.Frames.Where(f => f.Index == _project.CurrentFrameIndex).SelectMany(f => f.Strokes)))
                    if (st.Selected) st.FlipX = !st.FlipX;
                AnimCanvas.InvalidateVisual();
                SetDirty();
            };
            ToolSettings.FlipVRequested += (s, e) => {
                AnimCanvas.SaveUndoState();
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

            // Add frame to ACTIVE layer
            Timeline.FrameAdded += () => {
                var layer = _project.Layers.FirstOrDefault(l => l.Id == _project.ActiveLayerId);
                if (layer == null) return;
                
                // Add frame right after current playhead
                int newIndex = _project.CurrentFrameIndex + 1;
                if (newIndex >= 1000) return;
                
                // Shift subsequent frames if needed
                foreach (var f in layer.Frames.Where(f => f.Index >= newIndex).OrderByDescending(f => f.Index))
                    f.Index++;
                
                layer.Frames.Add(new AnimationFrame { Index = newIndex });
                _project.CurrentFrameIndex = newIndex;

                Timeline.RefreshTimeline();
                AnimCanvas.InvalidateVisual();
                SetDirty();
                UpdateStatus();
            };

            // Remove frame from ACTIVE layer
            Timeline.FrameRemoved += () => {
                var layer = _project.Layers.FirstOrDefault(l => l.Id == _project.ActiveLayerId);
                if (layer == null) return;

                var frameToRemove = layer.Frames.FirstOrDefault(f => f.Index == _project.CurrentFrameIndex);
                if (frameToRemove != null) layer.Frames.Remove(frameToRemove);
                
                // Shift back
                foreach (var f in layer.Frames.Where(f => f.Index > _project.CurrentFrameIndex))
                    f.Index--;

                Timeline.RefreshTimeline();
                AnimCanvas.InvalidateVisual();
                SetDirty();
                UpdateStatus();
            };

            // Duplicate frame on ACTIVE layer
            Timeline.FrameDuplicated += () => {
                var layer = _project.Layers.FirstOrDefault(l => l.Id == _project.ActiveLayerId);
                if (layer == null) return;
                
                int newIndex = _project.CurrentFrameIndex + 1;
                if (newIndex >= 1000) return;

                foreach (var f in layer.Frames.Where(f => f.Index >= newIndex).OrderByDescending(f => f.Index))
                    f.Index++;

                var sourceFrame = layer.Frames.FirstOrDefault(f => f.Index == _project.CurrentFrameIndex);
                var newFrame = new AnimationFrame { Index = newIndex };
                if (sourceFrame != null)
                {
                    newFrame.Strokes = sourceFrame.Strokes.Select(s => s.Clone()).ToList();
                }
                layer.Frames.Add(newFrame);

                _project.CurrentFrameIndex = newIndex;
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
            
            AnimCanvas.PointerReleased += (s, e) => SetDirty();
        }

        public MainWindow() : this(new AnimationProject()) { }

        private void SetDirty() { _isDirty = true; StatusSavedText.Text = "Unsaved changes"; }

        private void UpdateTimerInterval() { if (_project.FPS > 0) _playTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / _project.FPS); }

        private int GetMaxFrames()
        {
            if (!_project.Layers.Any()) return 0;
            return _project.Layers.Max(l => l.Frames.Count > 0 ? l.Frames.Max(f => f.Index) + 1 : 1);
        }

        private void PlayAnimation() { if (_isPlaying) return; _isPlaying = true; _playTimer.Start(); }
        private void PauseAnimation() { _isPlaying = false; _playTimer.Stop(); }
        private void StopAnimation() { PauseAnimation(); _project.CurrentFrameIndex = 0; Timeline.RefreshTimeline(); AnimCanvas.InvalidateVisual(); UpdateStatus(); }

        private void PlayTimer_Tick(object? sender, EventArgs e)
        {
            int maxFrames = GetMaxFrames();
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
                if (frame != null) frame.Strokes.Clear();
            }
            AnimCanvas.InvalidateVisual();
            SetDirty();
        }

        private void UpdateStatus()
        {
            int maxFrames = Math.Max(1, GetMaxFrames());
            StatusFrameText.Text = $"Frame: {_project.CurrentFrameIndex + 1} / {maxFrames}";
            StatusToolText.Text = $"Tool: {AnimCanvas.CurrentTools}";
        }

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
            if (StrokeColorBtn.Flyout is Flyout flyout) { StrokeColorPicker.BeginEdit(AnimCanvas.CurrentColor); flyout.ShowAt(StrokeColorBtn); }
        }

        private void FillColorBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (FillColorBtn.Flyout is Flyout flyout) { FillColorPicker.BeginEdit(AnimCanvas.FillColor); flyout.ShowAt(FillColorBtn); }
        }

        private void SwapColors_Click(object? sender, RoutedEventArgs e)
        {
            var temp = AnimCanvas.CurrentColor;
            AnimCanvas.CurrentColor = AnimCanvas.FillColor;
            AnimCanvas.FillColor = temp;
            StrokeColorBorder.Background = new SolidColorBrush(AnimCanvas.CurrentColor);
            FillColorBorder.Background = new SolidColorBrush(AnimCanvas.FillColor);
        }

        private async void MenuNewProject_Click(object? sender, RoutedEventArgs e)
        {
            if (_isDirty)
            {
                var saveResult = await SaveConfirmDialog.ShowSaveConfirmAsync(this);
                if (saveResult == SaveConfirmResult.Save)
                {
                    if (!string.IsNullOrEmpty(_currentFilePath)) await SaveProject(_currentFilePath);
                    else
                    {
                        var opts = new FilePickerSaveOptions { DefaultExtension = "ersma", FileTypeChoices = new[] { new FilePickerFileType("Ersm") { Patterns = new[] { "*.ersma" } } } };
                        var f = await StorageProvider.SaveFilePickerAsync(opts);
                        if (f == null) return;
                        await SaveProject(f.Path.LocalPath);
                    }
                }
                else if (saveResult == SaveConfirmResult.Cancel) return;
            }
            var welcome = new WelcomeWindow();
            welcome.Show();
            this.Close();
        }

        private async void MenuOpen_Click(object? sender, RoutedEventArgs e)
        {
            if (_isDirty)
            {
                var saveResult = await SaveConfirmDialog.ShowSaveConfirmAsync(this);
                if (saveResult == SaveConfirmResult.Save)
                {
                    if (!string.IsNullOrEmpty(_currentFilePath)) await SaveProject(_currentFilePath);
                    else
                    {
                        var opts = new FilePickerSaveOptions { DefaultExtension = "ersma", FileTypeChoices = new[] { new FilePickerFileType("Ersm") { Patterns = new[] { "*.ersma" } } } };
                        var f = await StorageProvider.SaveFilePickerAsync(opts);
                        if (f == null) return;
                        await SaveProject(f.Path.LocalPath);
                    }
                }
                else if (saveResult == SaveConfirmResult.Cancel) return;
            }

            var options = new FilePickerOpenOptions { FileTypeFilter = new[] { new FilePickerFileType("Ersm") { Patterns = new[] { "*.ersma" } } } };
            var result = await StorageProvider.OpenFilePickerAsync(options);
            if (result.Count > 0)
            {
                var p = await ProjectFileService.LoadProjectAsync(result[0].Path.LocalPath);
                if (p != null) { var mw = new MainWindow(p, result[0].Path.LocalPath); mw.Show(); this.Close(); }
            }
        }

        private async void MenuSave_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath)) await SaveAsAsync();
            else await SaveProject(_currentFilePath);
        }

        private async void MenuSaveAs_Click(object? sender, RoutedEventArgs e) => await SaveAsAsync();

        private async System.Threading.Tasks.Task SaveAsAsync()
        {
            var options = new FilePickerSaveOptions { DefaultExtension = "ersma", FileTypeChoices = new[] { new FilePickerFileType("Ersm") { Patterns = new[] { "*.ersma" } } } };
            var file = await StorageProvider.SaveFilePickerAsync(options);
            if (file != null) { _currentFilePath = file.Path.LocalPath; await SaveProject(_currentFilePath); }
        }

        private async System.Threading.Tasks.Task SaveProject(string path)
        {
            RightPanel.UpdateProject(_project);
            await ProjectFileService.SaveProjectAsync(_project, path);
            _isDirty = false;
            StatusSavedText.Text = "Saved successfully";
        }

        private void MenuExit_Click(object? sender, RoutedEventArgs e) => Close();

        private async void MenuExportMp4_Click(object? sender, RoutedEventArgs e)
        {
            var options = new FilePickerSaveOptions { DefaultExtension = "mp4", FileTypeChoices = new[] { new FilePickerFileType("MP4 Video") { Patterns = new[] { "*.mp4" } } } };
            var file = await StorageProvider.SaveFilePickerAsync(options);
            if (file != null)
            {
                StatusSavedText.Text = "Exporting MP4...";
                try { await Services.ExportService.ExportAnimationAsync(AnimCanvas, _project, file.Path.LocalPath, isGif: false); StatusSavedText.Text = "Export Complete."; }
                catch (System.Exception) { StatusSavedText.Text = "Export Failed (Is FFmpeg installed?)"; }
            }
        }

        private async void MenuExportGif_Click(object? sender, RoutedEventArgs e)
        {
            var options = new FilePickerSaveOptions { DefaultExtension = "gif", FileTypeChoices = new[] { new FilePickerFileType("GIF Animation") { Patterns = new[] { "*.gif" } } } };
            var file = await StorageProvider.SaveFilePickerAsync(options);
            if (file != null)
            {
                StatusSavedText.Text = "Exporting GIF...";
                try { await Services.ExportService.ExportAnimationAsync(AnimCanvas, _project, file.Path.LocalPath, isGif: true); StatusSavedText.Text = "Export Complete."; }
                catch (System.Exception) { StatusSavedText.Text = "Export Failed (Is FFmpeg installed?)"; }
            }
        }

        private async void Window_Closing(object? sender, WindowClosingEventArgs e)
        {
            if (_isDirty)
            {
                e.Cancel = true;
                _isDirty = false;

                var result = await SaveConfirmDialog.ShowSaveConfirmAsync(this);
                if (result == SaveConfirmResult.Save)
                {
                    if (string.IsNullOrEmpty(_currentFilePath))
                    {
                        var options = new FilePickerSaveOptions { DefaultExtension = "ersma", FileTypeChoices = new[] { new FilePickerFileType("Ersm") { Patterns = new[] { "*.ersma" } } } };
                        var file = await StorageProvider.SaveFilePickerAsync(options);
                        if (file != null) { await SaveProject(file.Path.LocalPath); Close(); }
                        else { _isDirty = true; }
                    }
                    else { await SaveProject(_currentFilePath); Close(); }
                }
                else if (result == SaveConfirmResult.DontSave) { Close(); }
                else { _isDirty = true; }
            }
        }

        private void MenuUndo_Click(object? sender, RoutedEventArgs e) => AnimCanvas.Undo();
        private void MenuRedo_Click(object? sender, RoutedEventArgs e) => AnimCanvas.Redo();
        private void MenuPlayPause_Click(object? sender, RoutedEventArgs e) { if (_isPlaying) PauseAnimation(); else PlayAnimation(); }
        private void MenuAddFrame_Click(object? sender, RoutedEventArgs e) 
        {
            if (!_project.Layers.Any()) return;
            if (GetMaxFrames() >= 1000) return;
            Timeline.BtnAddFrame.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); 
        }
        
        private void MenuAbout_Click(object? sender, RoutedEventArgs e) { }
    }
}