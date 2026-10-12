using System.Windows;
using Vexstrap.Resources;

namespace Vexstrap.Utility
{
    internal static class Shortcut
    {
        private static GenericTriState _loadStatus = GenericTriState.Unknown;

        public static void Create(string exePath, string exeArgs, string lnkPath, bool overwrite = false)
        {
            const string LOG_IDENT = "Shortcut::Create";

            if (!overwrite && File.Exists(lnkPath))
                return;

            try
            {
                string iconPath = exePath;

                if (App.Settings.Prop.BootstrapperIcon != Enums.BootstrapperIcon.IconVexstrap)
                {
                    string iconsDir = System.IO.Path.Combine(Paths.Base, "Icons");
                    string iconName = $"Shortcut_{App.Settings.Prop.BootstrapperIcon}.ico";
                    if (App.Settings.Prop.BootstrapperIcon == Enums.BootstrapperIcon.IconCustom)
                        iconName = "Shortcut_Custom.ico";
                    string customIconPath = System.IO.Path.Combine(iconsDir, iconName);
                    if (File.Exists(customIconPath))
                    {
                        iconPath = customIconPath;
                    }
                }

                ShellLink.Shortcut.CreateShortcut(exePath, exeArgs, iconPath, 0).WriteToFile(lnkPath);

                if (_loadStatus != GenericTriState.Successful)
                    _loadStatus = GenericTriState.Successful;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to create a shortcut for {lnkPath}!");
                App.Logger.WriteException(LOG_IDENT, ex);

                if (_loadStatus == GenericTriState.Failed)
                    return;

                _loadStatus = GenericTriState.Failed;

                Frontend.ShowMessageBox(Strings.Dialog_CannotCreateShortcuts, MessageBoxImage.Warning);
            }
        }

        public static void RefreshShortcuts()
        {
            try
            {
                if (App.Settings.Prop.BootstrapperIcon != Enums.BootstrapperIcon.IconVexstrap)
                {
                    string iconsDir = System.IO.Path.Combine(Paths.Base, "Icons");
                    System.IO.Directory.CreateDirectory(iconsDir);
                    string iconName = $"Shortcut_{App.Settings.Prop.BootstrapperIcon}.ico";
                    if (App.Settings.Prop.BootstrapperIcon == Enums.BootstrapperIcon.IconCustom)
                        iconName = "Shortcut_Custom.ico";
                    string shortcutIco = System.IO.Path.Combine(iconsDir, iconName);

                    if (App.Settings.Prop.BootstrapperIcon == Enums.BootstrapperIcon.IconCustom)
                    {
                        string customLoc = App.Settings.Prop.BootstrapperIconCustomLocation;
                        if (!string.IsNullOrEmpty(customLoc) && System.IO.File.Exists(customLoc))
                        {
                            System.IO.File.Copy(customLoc, shortcutIco, true);
                        }
                    }
                    else
                    {
                        using (var fs = new System.IO.FileStream(shortcutIco, System.IO.FileMode.Create))
                        {
                            Vexstrap.Extensions.BootstrapperIconEx.GetIcon(App.Settings.Prop.BootstrapperIcon).Save(fs);
                        }
                    }
                    Vexstrap.Utility.TaskbarIcon.NotifyShortcutChanged(shortcutIco);
                }

                var shortcuts = new (string lnkPath, string exeArgs)[]
                {
                    (System.IO.Path.Combine(Paths.Desktop, $"{App.ProjectName}.lnk"), ""),
                    (System.IO.Path.Combine(Paths.WindowsStartMenu, $"{App.ProjectName}.lnk"), ""),
                    (System.IO.Path.Combine(Paths.Desktop, $"{Strings.LaunchMenu_LaunchRoblox}.lnk"), "-player"),
                    (System.IO.Path.Combine(Paths.Desktop, $"{Strings.LaunchMenu_LaunchRobloxStudio}.lnk"), "-studio"),
                    (System.IO.Path.Combine(Paths.Desktop, $"{Strings.Menu_Title}.lnk"), "-settings")
                };

                foreach (var shortcut in shortcuts)
                {
                    if (System.IO.File.Exists(shortcut.lnkPath))
                    {
                        System.IO.File.Delete(shortcut.lnkPath);
                        Create(Paths.Application, shortcut.exeArgs, shortcut.lnkPath);
                        Vexstrap.Utility.TaskbarIcon.NotifyShortcutChanged(shortcut.lnkPath);
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("Shortcut::RefreshShortcuts", $"Failed to refresh shortcuts! {ex.Message}");
            }
        }

#if !DEBUG
        /// <summary>
        /// In Release builds: if "Vexstrap (TEST).lnk" exists on the Desktop, delete it and
        /// log its old TargetPath. Called on every startup, outside the signature check.
        /// </summary>
        public static void CleanupTestShortcut()
        {
            const string LOG_IDENT = "Shortcut::CleanupTestShortcut";

            try
            {
                string testLnkPath = System.IO.Path.Combine(Paths.Desktop, $"{App.ProjectName} (TEST).lnk");
                if (!System.IO.File.Exists(testLnkPath))
                    return;

                string oldTarget = "(unknown)";
                try
                {
                    var sc = ShellLink.Shortcut.ReadFromFile(testLnkPath);
                    oldTarget = sc.ExtraData.EnvironmentVariableDataBlock?.TargetUnicode ?? "(unreadable)";
                }
                catch { /* best-effort */ }

                System.IO.File.Delete(testLnkPath);
                App.Logger.WriteLine(LOG_IDENT, $"Deleted TEST shortcut (was pointing to: {oldTarget}).");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("Shortcut::CleanupTestShortcut", $"Failed to clean up TEST shortcut: {ex.Message}");
                App.Logger.WriteException("Shortcut::CleanupTestShortcut", ex);
            }
        }
#endif

#if DEBUG
        private static string? GetShortcutTarget(string lnkPath)
        {
            try
            {
                var sc = ShellLink.Shortcut.ReadFromFile(lnkPath);
                return sc.ExtraData.EnvironmentVariableDataBlock?.TargetUnicode;
            }
            catch
            {
                return null;
            }
        }

        private static string? GetShortcutArguments(string lnkPath)
        {
            try
            {
                var sc = ShellLink.Shortcut.ReadFromFile(lnkPath);
                return sc.StringData?.CommandLineArguments;
            }
            catch
            {
                return null;
            }
        }

        public static void ManageTestShortcut()
        {
            const string LOG_IDENT = "Shortcut::ManageTestShortcut";

            try
            {
                string testLnkPath = Path.Combine(Paths.Desktop, $"{App.ProjectName} (TEST).lnk");
                string liveLnkName = $"{App.ProjectName}.lnk";

                // Build the cap set: all Vexstrap*.lnk on Desktop EXCEPT shortcuts whose Arguments are exactly "-settings".
                string[] allMatches = Directory.GetFiles(Paths.Desktop, $"{App.ProjectName}*.lnk");
                string[] capSet = allMatches
                    .Where(f => !string.Equals(GetShortcutArguments(f), "-settings", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                // Cap is 2 (live + TEST). If already above 2, a stray is present: warn and do nothing.
                if (capSet.Length > 2)
                {
                    string fileList = string.Join(", ", capSet.Select(Path.GetFileName));
                    App.Logger.WriteLine(LOG_IDENT, $"Warning: Desktop shortcut cap exceeded ({fileList}). Skipping test shortcut management.");
                    return;
                }

                bool testExists = File.Exists(testLnkPath);

                if (testExists)
                {
                    // Refresh only when the target has changed.
                    string? currentTarget = GetShortcutTarget(testLnkPath);
                    if (string.Equals(currentTarget, Paths.Process, StringComparison.OrdinalIgnoreCase))
                    {
                        App.Logger.WriteLine(LOG_IDENT, $"TEST shortcut already points to {Paths.Process}. No update needed.");
                        return;
                    }

                    Create(Paths.Process, "", testLnkPath, overwrite: true);
                    App.Logger.WriteLine(LOG_IDENT, $"Refreshed TEST shortcut target to: {Paths.Process}");
                    return;
                }

                // TEST doesn't exist yet. Cap set must be count <= 1 and contain only "Vexstrap.lnk" (or be empty).
                bool canCreate = capSet.Length <= 1 && (capSet.Length == 0 || capSet.All(f =>
                    string.Equals(Path.GetFileName(f), liveLnkName, StringComparison.OrdinalIgnoreCase)));

                if (canCreate)
                {
                    Create(Paths.Process, "", testLnkPath);
                    App.Logger.WriteLine(LOG_IDENT, $"Created TEST shortcut pointing to: {Paths.Process}");
                }
                else
                {
                    string fileList = string.Join(", ", capSet.Select(Path.GetFileName));
                    App.Logger.WriteLine(LOG_IDENT, $"Warning: Desktop shortcut cap reached or unexpected Vexstrap shortcuts found ({fileList}). Skipping test shortcut creation.");
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to manage test shortcut: {ex.Message}");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }
#endif
    }
}
