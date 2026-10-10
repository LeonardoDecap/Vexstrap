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

        public bool AnalyticsEnabled
        {
            get => App.Settings.Prop.EnableAnalytics;
            set => App.Settings.Prop.EnableAnalytics = value;
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
                // turning it on starts the clock now, so the first run is one interval from here
                if (App.Settings.Prop.CleanupSchedule == CleanupFrequency.Off && value != CleanupFrequency.Off)
                    App.Settings.Prop.LastCleanupUtc = DateTime.UtcNow;

                App.Settings.Prop.CleanupSchedule = value;
                OnPropertyChanged(nameof(CleanupScheduleSelection));
            }
        }

        public ICommand CleanNowCommand => new RelayCommand(CleanNow);

        private void CleanNow()
        {
            long? freed = Vexstrap.Utility.Cleaner.Run();

            if (freed is null)
            {
                System.Windows.MessageBox.Show(Strings.Menu_Vexstrap_Cleanup_Blocked, App.ProjectName,
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            App.Settings.Prop.LastCleanupUtc = DateTime.UtcNow;
            App.Settings.Save();

            System.Windows.MessageBox.Show(string.Format(Strings.Menu_Vexstrap_Cleanup_Done, Vexstrap.Utility.Cleaner.FormatBytes(freed.Value)),
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

