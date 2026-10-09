using System.Windows;
using Vexstrap.Resources;

namespace Vexstrap.Utility
{
    internal static class Shortcut
    {
        private static GenericTriState _loadStatus = GenericTriState.Unknown;

        public static void Create(string exePath, string exeArgs, string lnkPath)
        {
            const string LOG_IDENT = "Shortcut::Create";

            if (File.Exists(lnkPath))
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

                // Also update any pinned taskbar shortcuts pointing to Vexstrap
                try
                {
                    string pinnedDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
                    string pinnedShortcut = System.IO.Path.Combine(pinnedDir, "Vexstrap.lnk");
                    if (System.IO.File.Exists(pinnedShortcut))
                    {
                        System.IO.File.Delete(pinnedShortcut);
                        Create(Paths.Application, "-menu", pinnedShortcut);
                        Vexstrap.Utility.TaskbarIcon.NotifyShortcutChanged(pinnedShortcut);
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException("Shortcut::RefreshShortcuts", ex);
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("Shortcut::RefreshShortcuts", $"Failed to refresh shortcuts! {ex.Message}");
            }
        }

    }
}