using System.Collections.ObjectModel;

namespace Vexstrap.Models.Persistable
{
    public class Settings
    {
        // Vexstrap configuration
        public BootstrapperStyle BootstrapperStyle { get; set; } = BootstrapperStyle.FluentDialog;

        private BootstrapperIcon _bootstrapperIcon = BootstrapperIcon.IconVexstrap;

        [JsonConverter(typeof(SafeBootstrapperIconConverter))]
        public BootstrapperIcon BootstrapperIcon
        {
            get => Enum.IsDefined(typeof(BootstrapperIcon), _bootstrapperIcon) ? _bootstrapperIcon : BootstrapperIcon.IconVexstrap;
            set => _bootstrapperIcon = Enum.IsDefined(typeof(BootstrapperIcon), value) ? value : BootstrapperIcon.IconVexstrap;
        }

        public string BootstrapperTitle { get; set; } = App.ProjectName;
        public string BootstrapperIconCustomLocation { get; set; } = "";
        public Theme Theme { get; set; } = Theme.Default;
        public string ThemePreset { get; set; } = "";
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool DeveloperMode { get; set; } = false;
        public bool CheckForUpdates { get; set; } = true;
        public string LastShortcutSignature { get; set; } = "";
        public CleanupFrequency CleanupSchedule { get; set; } = CleanupFrequency.Off;
        // Deprecated: Migrated to State.json (LastCleanupUtc). Preserved temporarily for backward compatibility; do not use.
        public DateTime LastCleanupUtc { get; set; } = default;
        public bool ConfirmLaunches { get; set; } = false;
        public string Locale { get; set; } = "nil";
        public bool UseFastFlagManager { get; set; } = true;
        public bool FastFlagWarningAcknowledged { get; set; } = false;
        public bool WPFSoftwareRender { get; set; } = false;
        public bool EnableAnalytics { get; set; } = false;
        public bool BackgroundUpdatesEnabled { get; set; } = false;
        public bool DebugDisableVersionPackageCleanup { get; set; } = false;
        public string? SelectedCustomTheme { get; set; } = null;
        public WebEnvironment WebEnvironment { get; set; } = WebEnvironment.Production;

        // integration configuration
        public bool EnableActivityTracking { get; set; } = true;
        public bool UseDiscordRichPresence { get; set; } = true;
        public bool HideRPCButtons { get; set; } = true;
        public bool ShowAccountOnRichPresence { get; set; } = false;
        public bool ShowServerDetails { get; set; } = false;
        public ObservableCollection<CustomIntegration> CustomIntegrations { get; set; } = new();


        // mod preset configuration
        public bool UseDisableAppPatch { get; set; } = false;
    }

    public class SafeBootstrapperIconConverter : JsonConverter<BootstrapperIcon>
    {
        public override BootstrapperIcon Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? str = reader.GetString();
                if (!string.IsNullOrEmpty(str) && Enum.TryParse<BootstrapperIcon>(str, true, out var icon) && Enum.IsDefined(typeof(BootstrapperIcon), icon))
                    return icon;

                return BootstrapperIcon.IconVexstrap;
            }

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int val))
            {
                var icon = (BootstrapperIcon)val;
                if (Enum.IsDefined(typeof(BootstrapperIcon), icon))
                    return icon;

                return BootstrapperIcon.IconVexstrap;
            }

            return BootstrapperIcon.IconVexstrap;
        }

        public override void Write(Utf8JsonWriter writer, BootstrapperIcon value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue((int)value);
        }
    }
}


