using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;

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

        public ICommand CleanNowCommand => new RelayCommand(CleanNow);

        private void CleanNow()
        {
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

