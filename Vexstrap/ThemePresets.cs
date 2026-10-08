using System.Windows;
using System.Windows.Media;

namespace Vexstrap
{
    public record ThemePreset(string Id, string Name, string AccentHex, string BackgroundHex, bool Dark)
    {
        public Color Accent => (Color)ColorConverter.ConvertFromString(AccentHex);
        public Color Background => (Color)ColorConverter.ConvertFromString(BackgroundHex);
    }

    public static class ThemePresets
    {
        public static readonly ThemePreset[] All =
        {
            new("vex",      "Vex",      "#B455E8", "#1E1527", true),
            new("midnight", "Midnight", "#7C83FF", "#131729", true),
            new("ocean",    "Ocean",    "#38BDF8", "#0E1D2A", true),
            new("mint",     "Mint",     "#2DD4A7", "#0E2420", true),
            new("ember",    "Ember",    "#FF6B3D", "#26160F", true),
            new("rose",     "Rose",     "#F472B6", "#27131E", true),
            new("mono",     "Mono",     "#A3A3A3", "#181818", true),
            new("sunrise",  "Sunrise",  "#F59E0B", "#FFF3DC", false),
            new("paper",    "Paper",    "#2563EB", "#ECF1FA", false),
        };

        public static ThemePreset? Find(string? id) =>
            string.IsNullOrEmpty(id) ? null : All.FirstOrDefault(p => p.Id == id);

        // WPF-UI theme brushes that make up the window and layer backgrounds.
        // Keys that don't exist in this build are harmless.
        private static readonly string[] BackgroundKeys =
        {
            "ApplicationBackgroundBrush",
            "SolidBackgroundFillColorBaseBrush",
            "SolidBackgroundFillColorSecondaryBrush",
            "SolidBackgroundFillColorTertiaryBrush",
            "LayerFillColorDefaultBrush",
            "LayerFillColorAltBrush"
        };

        public static void ApplyBackground(ThemePreset? preset, bool darkMode)
        {
            var res = Application.Current.Resources;

            // top-level keys win over the theme dictionary, so removing them restores stock colours
            foreach (var key in BackgroundKeys)
                res.Remove(key);

            res["VexWindowBackgroundBrush"] = Brushes.Transparent;

            if (preset is null)
                return;

            var bg = preset.Dark == darkMode ? preset.Background : Derive(preset.Accent, darkMode);
            var layer = Shift(bg, 0.04, darkMode);

            res["ApplicationBackgroundBrush"] = new SolidColorBrush(bg);
            res["VexWindowBackgroundBrush"] = new SolidColorBrush(bg);
            res["SolidBackgroundFillColorBaseBrush"] = new SolidColorBrush(bg);
            res["SolidBackgroundFillColorSecondaryBrush"] = new SolidColorBrush(layer);
            res["SolidBackgroundFillColorTertiaryBrush"] = new SolidColorBrush(layer);
            res["LayerFillColorDefaultBrush"] = new SolidColorBrush(layer);
            res["LayerFillColorAltBrush"] = new SolidColorBrush(layer);
        }

        // when the window theme doesn't match the preset (e.g. a dark preset on a light window),
        // derive a matching tint from the accent colour
        private static Color Derive(Color accent, bool darkMode)
        {
            byte f(byte v) => darkMode ? (byte)(18 + (v - 18) * 0.12) : (byte)(255 - (255 - v) * 0.10);
            return Color.FromRgb(f(accent.R), f(accent.G), f(accent.B));
        }

        // dark themes get lighter layers, light themes get slightly darker ones
        private static Color Shift(Color c, double amount, bool dark)
        {
            byte f(byte v) => (byte)Math.Clamp(v + (dark ? (255 - v) : -v) * amount, 0, 255);
            return Color.FromRgb(f(c.R), f(c.G), f(c.B));
        }
    }
}