using System.Drawing;

namespace Vexstrap.Extensions
{
    static class BootstrapperIconEx
    {
        public static IReadOnlyCollection<BootstrapperIcon> Selections => new BootstrapperIcon[]
        {
            BootstrapperIcon.IconVexstrap,
            BootstrapperIcon.Icon2022,
            BootstrapperIcon.Icon2019,
            BootstrapperIcon.Icon2017,
            BootstrapperIcon.IconLate2015,
            BootstrapperIcon.IconEarly2015,
            BootstrapperIcon.Icon2011,
            BootstrapperIcon.Icon2008,
            BootstrapperIcon.IconThemeCyberpunkCyan,
            BootstrapperIcon.IconThemeEmeraldMint,
            BootstrapperIcon.IconThemeSunsetBlaze,
            BootstrapperIcon.IconThemeObsidianStealth,
            BootstrapperIcon.IconThemeBloodRuby,
            BootstrapperIcon.IconThemeNordicFrost,
            BootstrapperIcon.IconThemeMidnightAmethyst,
            BootstrapperIcon.IconThemeSolarFlare,
            BootstrapperIcon.Icon3DCrimsonOrange,
            BootstrapperIcon.Icon3DCyberpunkCyan,
            BootstrapperIcon.Icon3DEmeraldMint,
            BootstrapperIcon.Icon3DStealthObsidian,
            BootstrapperIcon.Icon3DSunsetBlaze,
            BootstrapperIcon.IconCustom
        };

        // small note on handling icon sizes
        // i'm using multisize icon packs here with sizes 16, 24, 32, 48, 64 and 128
        // use this for generating multisize packs: https://www.aconvert.com/icon/

        public static Icon GetIcon(this BootstrapperIcon icon)
        {
            const string LOG_IDENT = "BootstrapperIconEx::GetIcon";

            // load the custom icon file
            if (icon == BootstrapperIcon.IconCustom)
            {
                Icon? customIcon = null;
                string location = App.Settings.Prop.BootstrapperIconCustomLocation;

                if (String.IsNullOrEmpty(location)) 
                {
                    App.Logger.WriteLine(LOG_IDENT, "Warning: custom icon is not set.");
                }
                else
                {
                    try
                    {
                        customIcon = new Icon(location);
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine(LOG_IDENT, $"Failed to load custom icon!");
                        App.Logger.WriteException(LOG_IDENT, ex);
                    }
                }

                return customIcon ?? Properties.Resources.IconVexstrap;
            }

            return icon switch
            {
                BootstrapperIcon.IconVexstrap => Properties.Resources.IconVexstrap,
                BootstrapperIcon.Icon2008 => Properties.Resources.Icon2008,
                BootstrapperIcon.Icon2011 => Properties.Resources.Icon2011,
                BootstrapperIcon.IconEarly2015 => Properties.Resources.IconEarly2015,
                BootstrapperIcon.IconLate2015 => Properties.Resources.IconLate2015,
                BootstrapperIcon.Icon2017 => Properties.Resources.Icon2017,
                BootstrapperIcon.Icon2019 => Properties.Resources.Icon2019,
                BootstrapperIcon.Icon2022 => Properties.Resources.Icon2022,
                BootstrapperIcon.IconVexstrapClassic => Properties.Resources.IconVexstrapClassic,
                BootstrapperIcon.IconThemeCyberpunkCyan => Properties.Resources.IconThemeCyberpunkCyan,
                BootstrapperIcon.IconThemeEmeraldMint => Properties.Resources.IconThemeEmeraldMint,
                BootstrapperIcon.IconThemeSunsetBlaze => Properties.Resources.IconThemeSunsetBlaze,
                BootstrapperIcon.IconThemeObsidianStealth => Properties.Resources.IconThemeObsidianStealth,
                BootstrapperIcon.IconThemeBloodRuby => Properties.Resources.IconThemeBloodRuby,
                BootstrapperIcon.IconThemeNordicFrost => Properties.Resources.IconThemeNordicFrost,
                BootstrapperIcon.IconThemeMidnightAmethyst => Properties.Resources.IconThemeMidnightAmethyst,
                BootstrapperIcon.IconThemeSolarFlare => Properties.Resources.IconThemeSolarFlare,
                BootstrapperIcon.Icon3DCrimsonOrange => Properties.Resources.Icon3DCrimsonOrange,
                BootstrapperIcon.Icon3DCyberpunkCyan => Properties.Resources.Icon3DCyberpunkCyan,
                BootstrapperIcon.Icon3DEmeraldMint => Properties.Resources.Icon3DEmeraldMint,
                BootstrapperIcon.Icon3DStealthObsidian => Properties.Resources.Icon3DStealthObsidian,
                BootstrapperIcon.Icon3DSunsetBlaze => Properties.Resources.Icon3DSunsetBlaze,
                _ => Properties.Resources.IconVexstrap
            };
        }
    }
}

