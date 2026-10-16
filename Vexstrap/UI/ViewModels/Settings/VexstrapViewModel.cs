using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using Vexstrap.UI.Elements.Dialogs;

namespace Vexstrap.UI.ViewModels.Settings
{
    public class VexstrapViewModel : NotifyPropertyChangedViewModel
    {
        public WebEnvironment[] WebEnvironments => Enum.GetValues<WebEnvironment>();

        public bool UpdateCheckingEnabled
        {
            get => App.Settings.Prop.CheckForUpdates;
            set => App.Settings.Prop.CheckForUpdates = value;
        }


        public WebEnvironment WebEnvironment
        {
            get => App.Settings.Prop.WebEnvironment;
            set => App.Settings.Prop.WebEnvironment = value;
        }

        public Visibility WebEnvironmentVisibility => App.Settings.Prop.DeveloperMode ? Visibility.Visible : Visibility.Collapsed;

        public bool ShouldExportConfig { get; set; } = true;

        public bool ShouldExportLogs { get; set; } = true;

        private readonly KeyValuePair<CleanupFrequency, string>[] _cleanupOptions =
        {
            new KeyValuePair<CleanupFrequency, string>(CleanupFrequency.Off, Strings.Menu_Vexstrap_Cleanup_ScheduleOff),
            new KeyValuePair<CleanupFrequency, string>(CleanupFrequency.Daily, Strings.Menu_Vexstrap_Cleanup_ScheduleDaily),
            new KeyValuePair<CleanupFrequency, string>(CleanupFrequency.Weekly, Strings.Menu_Vexstrap_Cleanup_ScheduleWeekly),
            new KeyValuePair<CleanupFrequency, string>(CleanupFrequency.Monthly, Strings.Menu_Vexstrap_Cleanup_ScheduleMonthly),
        };

        public KeyValuePair<CleanupFrequency, string>[] CleanupScheduleOptions => _cleanupOptions;

        public CleanupFrequency CleanupScheduleSelection
        {
            get => App.Settings.Prop.CleanupSchedule;
            set
            {
                if (App.Settings.Prop.CleanupSchedule != value)
                {
                    if (ReinstallGate.IsRunning)
                    {
                        OnPropertyChanged(nameof(CleanupScheduleSelection));
                        return;
                    }

                    // A new schedule starts the clock from now. The first cleanup runs one interval later.
                    if (value != CleanupFrequency.Off)
                    {
                        App.State.Prop.LastCleanupUtc = DateTime.UtcNow;
                        App.State.Save();
                    }

                    App.Settings.Prop.CleanupSchedule = value;
                    OnPropertyChanged(nameof(CleanupScheduleSelection));
                }
            }
        }

        public ICommand InstallLatestVersionCommand { get; } = new AsyncRelayCommand(Vexstrap.Installer.InstallLatestVersion);

        public ICommand ViewPlayerLocationCommand => new RelayCommand(() => ViewInstallLocation(false));
        public ICommand ViewStudioLocationCommand => new RelayCommand(() => ViewInstallLocation(true));

        public ICommand CleanNowCommand => new RelayCommand(CleanNow);

        private void CleanNow()
        {
            if (ReinstallGate.IsRunning)
                return;

            CleanupResult result = Vexstrap.Utility.Cleaner.Run();

            if (result.IsBlocked || result.VersionCleanupBlocked)
            {
                System.Windows.MessageBox.Show(Strings.Menu_Vexstrap_Cleanup_Blocked, App.ProjectName,
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (result.FailedEntries > 0)
            {
                System.Windows.MessageBox.Show(
                    string.Format(Strings.Menu_Vexstrap_Cleanup_PartialFailure, Vexstrap.Utility.Cleaner.FormatBytes(result.BytesFreed), result.FailedEntries),
                    App.ProjectName,
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            App.State.Prop.LastCleanupUtc = DateTime.UtcNow;
            App.State.Save();

            System.Windows.MessageBox.Show(string.Format(Strings.Menu_Vexstrap_Cleanup_Done, Vexstrap.Utility.Cleaner.FormatBytes(result.BytesFreed)),
                App.ProjectName, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private static bool TryGetInstallPath(bool isStudio, out string folderPath, out bool isStockInstall)
        {
            folderPath = string.Empty;
            isStockInstall = false;

            string stateFile = isStudio ? App.StudioState.FileLocation : App.PlayerState.FileLocation;
            string exeName = isStudio ? "RobloxStudioBeta.exe" : "RobloxPlayerBeta.exe";
            string uninstallSubKey = isStudio
                ? @"Software\Microsoft\Windows\CurrentVersion\Uninstall\roblox-studio"
                : @"Software\Microsoft\Windows\CurrentVersion\Uninstall\roblox-player";

            if (File.Exists(stateFile))
            {
                try
                {
                    string json = File.ReadAllText(stateFile);
                    DistributionState? tempState = JsonSerializer.Deserialize<DistributionState>(json);

                    if (!string.IsNullOrWhiteSpace(tempState?.VersionGuid))
                    {
                        string versionDir = Path.Combine(Paths.Versions, tempState.VersionGuid);
                        if (File.Exists(Path.Combine(versionDir, exeName)))
                        {
                            folderPath = versionDir;
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine("VexstrapViewModel::TryGetInstallPath", $"Failed to read state file '{stateFile}'");
                    App.Logger.WriteException("VexstrapViewModel::TryGetInstallPath", ex);
                }
            }

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(uninstallSubKey);
                if (key?.GetValue("InstallLocation") is string stockFolder && !string.IsNullOrWhiteSpace(stockFolder))
                {
                    if (File.Exists(Path.Combine(stockFolder, exeName)))
                    {
                        folderPath = stockFolder;
                        isStockInstall = true;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("VexstrapViewModel::TryGetInstallPath", $"Failed to read registry fallback '{uninstallSubKey}'");
                App.Logger.WriteException("VexstrapViewModel::TryGetInstallPath", ex);
            }

            return false;
        }

        private static bool TryReloadJsonState<T>(JsonManager<T> manager, out string? errorMessage) where T : class, new()
        {
            errorMessage = null;

            if (!File.Exists(manager.FileLocation))
                return true;

            try
            {
                string contents = File.ReadAllText(manager.FileLocation);
                T? parsed = JsonSerializer.Deserialize<T>(contents);

                if (parsed is null)
                    throw new ArgumentNullException("Deserialization returned null");

                manager.Prop = parsed;
                return true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("VexstrapViewModel::TryReloadJsonState", $"Failed to reload '{manager.FileLocation}'");
                App.Logger.WriteException("VexstrapViewModel::TryReloadJsonState", ex);
                errorMessage = $"{manager.FileName}: {ex.Message}";
                return false;
            }
        }

        private static void RefreshStatesAfterReinstall()
        {
            var errors = new List<string>();

            if (!TryReloadJsonState(App.PlayerState, out string? playerErr) && playerErr is not null)
                errors.Add(playerErr);

            if (!TryReloadJsonState(App.StudioState, out string? studioErr) && studioErr is not null)
                errors.Add(studioErr);

            if (!TryReloadJsonState(App.State, out string? stateErr) && stateErr is not null)
                errors.Add(stateErr);

            if (errors.Count > 0)
            {
                string warning = string.Format(
                    Strings.Menu_Vexstrap_InstallLocations_RefreshFailed,
                    string.Join("\n", errors));
                Frontend.ShowMessageBox(warning, MessageBoxImage.Warning);
            }
        }

        private void ViewInstallLocation(bool isStudio)
        {
            if (ReinstallGate.IsRunning)
                return;

            string productName = isStudio
                ? Strings.Menu_Vexstrap_InstallLocations_Studio
                : Strings.Menu_Vexstrap_InstallLocations_Player;

            if (TryGetInstallPath(isStudio, out string foundPath, out bool isStockInstall))
            {
                string template = isStockInstall
                    ? Strings.Menu_Vexstrap_InstallLocations_Found_Stock_Message
                    : Strings.Menu_Vexstrap_InstallLocations_Found_Message;

                string message = string.Format(template, productName, foundPath);

                var dialog = new FluentMessageBox(message, MessageBoxImage.Information, MessageBoxButton.OKCancel);
                dialog.ButtonOne.Content = Strings.Menu_Vexstrap_InstallLocations_Found_Copy;
                dialog.ButtonTwo.Content = Strings.Common_Close;
                dialog.ShowDialog();

                if (dialog.Result == MessageBoxResult.OK)
                {
                    try
                    {
                        Clipboard.SetDataObject(foundPath);
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine("VexstrapViewModel::ViewInstallLocation", "Failed to copy install path to clipboard");
                        App.Logger.WriteException("VexstrapViewModel::ViewInstallLocation", ex);
                    }
                }

                return;
            }

            string missingPrompt = string.Format(Strings.Menu_Vexstrap_InstallLocations_Missing_Message, productName);
            if (Frontend.ShowMessageBox(missingPrompt, MessageBoxImage.Question, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            LaunchReinstall(isStudio);
        }

        private void LaunchReinstall(bool isStudio)
        {
            if (ReinstallGate.IsRunning)
                return;

            string mode = isStudio ? "Studio" : "Player";

            foreach (string name in Vexstrap.Utility.Cleaner.RobloxProcessNames)
            {
                var procs = Process.GetProcessesByName(name);
                if (procs.Length > 0)
                {
                    foreach (var p in procs)
                        p.Dispose();

                    Frontend.ShowMessageBox(Strings.Menu_Vexstrap_InstallLocations_Blocked_RobloxRunning, MessageBoxImage.Warning);
                    return;
                }
            }

            string bootstrapperMutex = $"Vexstrap-Bootstrapper-{mode}";
            string updaterMutex = $"Vexstrap-BackgroundUpdater-{mode}";
            if (Utilities.DoesMutexExist(bootstrapperMutex) || Utilities.DoesMutexExist(updaterMutex))
            {
                Frontend.ShowMessageBox(Strings.Menu_Vexstrap_InstallLocations_Blocked_UpdaterRunning, MessageBoxImage.Warning);
                return;
            }

            ReinstallGate.Set(true);

            Process? process = null;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = Paths.Process,
                    Arguments = isStudio ? "-studio -nolaunch" : "-player -nolaunch",
                    UseShellExecute = false
                };

                process = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                process.Exited += (sender, args) =>
                {
                    process.Dispose();

                    var dispatcher = Application.Current?.Dispatcher;
                    if (dispatcher is null)
                    {
                        ReinstallGate.Set(false);
                        return;
                    }

                    dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            RefreshStatesAfterReinstall();
                        }
                        catch (Exception ex)
                        {
                            App.Logger.WriteLine("VexstrapViewModel::LaunchReinstall", $"Failed to refresh states after {mode} reinstall");
                            App.Logger.WriteException("VexstrapViewModel::LaunchReinstall", ex);
                        }
                        finally
                        {
                            ReinstallGate.Set(false);
                        }
                    });
                };

                if (!process.Start())
                {
                    App.Logger.WriteLine("VexstrapViewModel::LaunchReinstall", $"Process.Start returned false for {mode}");
                    process.Dispose();
                    ReinstallGate.Set(false);
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("VexstrapViewModel::LaunchReinstall", $"Failed to start reinstall process for {mode}");
                App.Logger.WriteException("VexstrapViewModel::LaunchReinstall", ex);
                process?.Dispose();
                ReinstallGate.Set(false);
            }
        }

        public ICommand ExportDataCommand => new RelayCommand(ExportData);

        private void ExportData()
        {
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");

            var dialog = new SaveFileDialog 
            { 
                FileName = $"Vexstrap-export-{timestamp}.zip",
                Filter = $"{Strings.FileTypes_ZipArchive}|*.zip" 
            };

            if (dialog.ShowDialog() != true)
                return;

            using var memStream = new MemoryStream();
            using var zipStream = new ZipOutputStream(memStream);

            if (ShouldExportConfig)
            {
                var files = new List<string>()
                {
                    App.Settings.FileLocation,
                    App.State.FileLocation,
                    App.FastFlags.FileLocation
                };

                AddFilesToZipStream(zipStream, files, "Config/");
            }

            if (ShouldExportLogs && Directory.Exists(Paths.Logs))
            {
                var files = Directory.GetFiles(Paths.Logs)
                    .Where(x => !x.Equals(App.Logger.FileLocation, StringComparison.OrdinalIgnoreCase));

                AddFilesToZipStream(zipStream, files, "Logs/");
            }

            zipStream.CloseEntry();
            zipStream.Finish();
            memStream.Position = 0;

            using var outputStream = File.OpenWrite(dialog.FileName);
            memStream.CopyTo(outputStream);

            Process.Start("explorer.exe", $"/select,\"{dialog.FileName}\"");
        }

        private void AddFilesToZipStream(ZipOutputStream zipStream, IEnumerable<string> files, string directory)
        {
            const string LOG_IDENT = "VexstrapViewModel::AddFilesToZipStream";

            foreach (string file in files)
            {
                if (!File.Exists(file))
                    continue;

                try
                {
                    using FileStream fileStream = File.OpenRead(file);

                    var entry = new ZipEntry(directory + Path.GetFileName(file));
                    entry.DateTime = DateTime.Now;

                    zipStream.PutNextEntry(entry);

                    fileStream.CopyTo(zipStream);
                }
                catch (IOException ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Failed to open '{file}'");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            }
        }
    }
}

