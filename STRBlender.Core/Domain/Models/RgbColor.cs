namespace STRBlender.Core.Domain.Models
{
    /// Plain RGB color value, used in place of System.Drawing.Color — which is
    /// Windows-only and unsupported on Blazor WebAssembly — so Core stays usable
    /// from any presentation layer. Each UI translates this into its own native
    /// color type (e.g. WPF's System.Windows.Media.Color, or a CSS hex string).
    /// Name mirrors System.Drawing.Color.Name's format for the presets below,
    /// since some callers (e.g. EpgPlotter's noise model) switch on it directly.
    public record RgbColor(byte R, byte G, byte B, string Name = "")
    {
        public static readonly RgbColor Blue = new(0, 0, 255, "Blue");
        public static readonly RgbColor Green = new(0, 128, 0, "Green");
        public static readonly RgbColor Goldenrod = new(218, 165, 32, "Goldenrod");
        public static readonly RgbColor Red = new(255, 0, 0, "Red");
        public static readonly RgbColor Purple = new(128, 0, 128, "Purple");
    }
}
