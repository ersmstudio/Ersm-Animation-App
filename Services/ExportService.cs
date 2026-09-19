using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Ersm_Animation_App.Models;
using FFMpegCore;
using FFMpegCore.Enums;

namespace Ersm_Animation_App.Services
{
    public static class ExportService
    {
        public static async Task ExportAnimationAsync(DrawingCanvas canvas, AnimationProject project, string outputPath, bool isGif)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!File.Exists(Path.Combine(baseDir, "ffmpeg.exe")))
            {
                await Xabe.FFmpeg.Downloader.FFmpegDownloader.GetLatestVersion(Xabe.FFmpeg.Downloader.FFmpegVersion.Official, baseDir);
            }
            
            GlobalFFOptions.Configure(new FFOptions { BinaryFolder = baseDir });

            if (!project.Layers.Any()) return;
            int maxFrames = project.Layers.Max(l => l.Frames.Count);
            if (maxFrames == 0) return;

            string tempFolder = Path.Combine(Path.GetTempPath(), "ErsmExport_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempFolder);

            try
            {
                var originalFrame = project.CurrentFrameIndex;

                for (int i = 0; i < maxFrames; i++)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        project.CurrentFrameIndex = i;
                        canvas.InvalidateVisual();
                        canvas.UpdateLayout();
                    });

                    await Task.Delay(50);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var pixelSize = new PixelSize((int)canvas.Bounds.Width, (int)canvas.Bounds.Height);
                        if (pixelSize.Width == 0 || pixelSize.Height == 0) return;

                        using var rtb = new RenderTargetBitmap(pixelSize, new Vector(96, 96));
                        rtb.Render(canvas);
                        
                        string framePath = Path.Combine(tempFolder, $"frame_{i:D4}.png");
                        rtb.Save(framePath);
                    });
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    project.CurrentFrameIndex = originalFrame;
                    canvas.InvalidateVisual();
                });

                if (isGif)
                {
                    await FFMpegArguments
                        .FromFileInput(Path.Combine(tempFolder, "frame_%04d.png"), false, options => options
                            .WithFramerate(project.FPS))
                        .OutputToFile(outputPath, false, options => options
                            .WithCustomArgument("-vf \"split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\"")
                            .WithFramerate(project.FPS))
                        .ProcessAsynchronously();
                }
                else
                {
                    await FFMpegArguments
                        .FromFileInput(Path.Combine(tempFolder, "frame_%04d.png"), false, options => options
                            .WithFramerate(project.FPS))
                        .OutputToFile(outputPath, false, options => options
                            .WithVideoCodec(VideoCodec.LibX264)
                            .WithFramerate(project.FPS))
                        .ProcessAsynchronously();
                }
            }
            finally
            {
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
            }
        }
    }
}
