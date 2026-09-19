using Avalonia.Controls;
using Ersm_Animation_App.Models;

namespace Ersm_Animation_App.Controls
{
    public partial class RightPanel : UserControl
    {
        public RightPanel()
        {
            InitializeComponent();
        }

        public void BindProject(AnimationProject project)
        {
            ProjWidth.Value = project.CanvasWidth;
            ProjHeight.Value = project.CanvasHeight;
            ProjFPS.Value = project.FPS;
            ProjLoop.IsChecked = project.IsLooping;
            ProjLoopStart.Value = project.LoopStartFrame;
            ProjLoopEnd.Value = project.LoopEndFrame;
        }

        public void UpdateProject(AnimationProject project)
        {
            project.CanvasWidth = (int)(ProjWidth.Value ?? 1920);
            project.CanvasHeight = (int)(ProjHeight.Value ?? 1080);
            project.FPS = (int)(ProjFPS.Value ?? 24);
            project.IsLooping = ProjLoop.IsChecked ?? false;
            project.LoopStartFrame = (int)(ProjLoopStart.Value ?? 0);
            project.LoopEndFrame = (int)(ProjLoopEnd.Value ?? 0);
        }
    }
}
