namespace Vexstrap.Models.Persistable
{
    public class State
    {
        public bool PromptWebView2Install { get; set; } = true;

        public bool ForceReinstall { get; set; } = false;

        public WindowState SettingsWindow { get; set; } = new();

        public DateTime LastCleanupUtc { get; set; } = default;

        public bool CleanupTimestampMigrated { get; set; } = false;
    }
}

