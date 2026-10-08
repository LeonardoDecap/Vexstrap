namespace Vexstrap
{
    public record ThemePreset(string Id, string Name, string AccentHex, bool Dark)
    {
        public System.Windows.Media.Color Accent =>
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(AccentHex);
    }

    public static class ThemePresets
    {
        public static readonly ThemePreset[] All =
        {
            new("vex",      "Vex",      "#B455E8", true),
            new("midnight", "Midnight", "#7C83FF", true),
            new("ocean",    "Ocean",    "#38BDF8", true),
            new("mint",     "Mint",     "#2DD4A7", true),
            new("ember",    "Ember",    "#FF6B3D", true),
            new("rose",     "Rose",     "#F472B6", true),
            new("mono",     "Mono",     "#A3A3A3", true),
            new("sunrise",  "Sunrise",  "#F59E0B", false),
            new("paper",    "Paper",    "#2563EB", false),
        };

        public static ThemePreset? Find(string? id) =>
            string.IsNullOrEmpty(id) ? null : All.FirstOrDefault(p => p.Id == id);
    }
}