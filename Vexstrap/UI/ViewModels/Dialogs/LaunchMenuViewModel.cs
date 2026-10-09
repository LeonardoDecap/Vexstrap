using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;

using Vexstrap.UI.Elements.About;

namespace Vexstrap.UI.ViewModels.Installer
{
    public class LaunchMenuViewModel : NotifyPropertyChangedViewModel
    {
        public string Version => string.Format(Strings.Menu_About_Version, App.Version);
        public System.Windows.Media.ImageSource WindowIcon => App.Settings.Prop.BootstrapperIcon.GetIcon().GetImageSource();

        private Brush _versionColor = Brushes.Gray;
        public Brush VersionColor
        {
            get => _versionColor;
            set { _versionColor = value; OnPropertyChanged(nameof(VersionColor)); }
        }

        private string _updateStatusText = "Checking...";
        public string UpdateStatusText
        {
            get => _updateStatusText;
            set { _updateStatusText = value; OnPropertyChanged(nameof(UpdateStatusText)); }
        }

        private Brush _updateStatusColor = Brushes.Gray;
        public Brush UpdateStatusColor
        {
            get => _updateStatusColor;
            set { _updateStatusColor = value; OnPropertyChanged(nameof(UpdateStatusColor)); }
        }

        public LaunchMenuViewModel()
        {
            CheckForUpdatesAsync();
        }

        private async void CheckForUpdatesAsync()
        {
            var release = await App.GetLatestRelease();
            if (release == null)
            {
                UpdateStatusText = "Error";
                VersionColor = Brushes.Gray;
                UpdateStatusColor = Brushes.Gray;
                return;
            }

            bool isUpdated = Vexstrap.Utilities.CompareVersions(App.Version, release.TagName) != Vexstrap.Enums.VersionComparison.LessThan;
            
            if (isUpdated)
            {
                VersionColor = Brushes.LightGreen;
                UpdateStatusText = "Yes";
                UpdateStatusColor = Brushes.LightGreen;
            }
            else
            {
                VersionColor = Brushes.Red;
                UpdateStatusText = "No";
                UpdateStatusColor = Brushes.Red;
            }
        }

        public ICommand LaunchSettingsCommand => new RelayCommand(LaunchSettings);

        public ICommand LaunchRobloxCommand => new RelayCommand(LaunchRoblox);

        public ICommand LaunchRobloxStudioCommand => new RelayCommand(LaunchRobloxStudio);

        public ICommand LaunchAboutCommand => new RelayCommand(LaunchAbout);

        public event EventHandler<NextAction>? CloseWindowRequest;

        private void LaunchSettings() => CloseWindowRequest?.Invoke(this, NextAction.LaunchSettings);

        private void LaunchRoblox() => CloseWindowRequest?.Invoke(this, NextAction.LaunchRoblox);

        private void LaunchRobloxStudio() => CloseWindowRequest?.Invoke(this, NextAction.LaunchRobloxStudio);

        private void LaunchAbout() => new MainWindow().ShowDialog();
    }
}
