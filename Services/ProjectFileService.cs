using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Ersm_Animation_App.Models;

namespace Ersm_Animation_App.Services
{
    public class ColorJsonConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (Color.TryParse(value, out var color))
                return color;
            return Colors.Black;
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class PointJsonConverter : JsonConverter<Point>
    {
        public override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (str != null)
            {
                var parts = str.Split(',');
                if (parts.Length == 2 && double.TryParse(parts[0], out double x) && double.TryParse(parts[1], out double y))
                {
                    return new Point(x, y);
                }
            }
            return new Point();
        }

        public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.X},{value.Y}");
        }
    }

    public static class ProjectFileService
    {
        private static JsonSerializerOptions GetOptions()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            options.Converters.Add(new ColorJsonConverter());
            options.Converters.Add(new PointJsonConverter());
            return options;
        }

        public static async Task SaveProjectAsync(AnimationProject project, string filePath)
        {
            using var fileStream = new FileStream(filePath, FileMode.Create);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, true);

            var projectEntry = archive.CreateEntry("project.json");
            using var entryStream = projectEntry.Open();
            
            await JsonSerializer.SerializeAsync(entryStream, project, GetOptions());
        }

        public static async Task<AnimationProject?> LoadProjectAsync(string filePath)
        {
            using var fileStream = new FileStream(filePath, FileMode.Open);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);

            var projectEntry = archive.GetEntry("project.json");
            if (projectEntry == null) return null;

            using var entryStream = projectEntry.Open();
            var project = await JsonSerializer.DeserializeAsync<AnimationProject>(entryStream, GetOptions());
            
            return project;
        }
    }
}
