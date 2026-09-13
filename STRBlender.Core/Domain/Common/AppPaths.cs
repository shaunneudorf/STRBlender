using System.IO;

namespace STRBlender.Core.Domain.Common
{
    public static class AppPaths
    {
        public static string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;

        public static string DataDirectory
        {
            get
            {
                var candidates = new[]
                {
                    Path.Combine(BaseDirectory, "data"),
                    Path.Combine(Directory.GetParent(BaseDirectory)?.FullName ?? BaseDirectory, "data"),
                    Path.Combine(Directory.GetCurrentDirectory(), "data")
                };

                foreach (var path in candidates)
                    if (Directory.Exists(path)) return path;

                return candidates[0]; // fallback
            }
        }

        public static string GetFrequencyFilePath(string filename) =>
            Path.Combine(DataDirectory, "frequencies", filename);

        public static string ReverseStutterPath => Path.Combine(DataDirectory, "stutter", "reverse_stutter.csv");
        public static string ForwardStutterPath => Path.Combine(DataDirectory, "stutter", "forward_stutter.csv");
    }
}