using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;

using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;

using Vexstrap.UI.ViewModels.Settings;
using Vexstrap.UI.ViewModels;

namespace Vexstrap.UI.Elements.Settings
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INavigationWindow
    {
        private Models.Persistable.WindowState _state => App.State.Prop.SettingsWindow;
        private readonly DispatcherTimer _reinstallTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
        private bool _wasReinstallRunning;
        private DateTime _nextRefreshAttempt = DateTime.MinValue;

        public MainWindow(bool showAlreadyRunningWarning)
        {
            var viewModel = new MainWindowViewModel();

            viewModel.RequestSaveNoticeEvent += (_, _) => 
            {
                SettingsSavedSnackbar.Show();
                Vexstrap.Utility.TaskbarIcon.Apply(this, Vexstrap.Utility.TaskbarIcon.GetCurrentIconPath());
            };
            viewModel.RequestCloseWindowEvent += (_, _) => Close();

            DataContext = viewModel;
            
            
            InitializeComponent();
            RootFrame.Navigated += RootFrame_Navigated;
            ReinstallGate.BeginMonitoringGeneration(App.ReinstallGenerationAtLoad);
            ReinstallGate.Changed += ReinstallGate_Changed;
            _reinstallTimer.Tick += ReinstallGate_Changed;
            _wasReinstallRunning = App.StateLoadedDuringReinstall || ReinstallGate.IsOperationRunning ||
                StateChangedSinceLoad(App.State) || StateChangedSinceLoad(App.PlayerState) || StateChangedSinceLoad(App.StudioState);
            ReinstallGate_Changed(this, EventArgs.Empty);
            _reinstallTimer.Start();
            



            App.Logger.WriteLine("MainWindow", "Initializing settings window");

            if (showAlreadyRunningWarning)
                ShowAlreadyRunningSnackbar();

            LoadState();
        }

        
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Vexstrap.Utility.TaskbarIcon.Apply(this, Vexstrap.Utility.TaskbarIcon.GetCurrentIconPath());
        }

        public void LoadState()
        {
            if (_state.Left > SystemParameters.VirtualScreenWidth)
                _state.Left = 0;

            if (_state.Top > SystemParameters.VirtualScreenHeight)
                _state.Top = 0;

            if (_state.Width > 0)
                this.Width = _state.Width;

            if (_state.Height > 0)
                this.Height = _state.Height;

            if (_state.Left > 0 && _state.Top > 0)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Left = _state.Left;
                this.Top = _state.Top;
            }
        }

        private async void ShowAlreadyRunningSnackbar()
        {
            await Task.Delay(500); // wait for everything to finish loading
            AlreadyRunningSnackbar.Show();
        }

        private static bool StateChangedSinceLoad<T>(JsonManager<T> manager) where T : class, new()
        {
            if (!manager.Loaded)
                return false;
            try
            {
                return manager.IsSaved ? manager.HasFileOnDiskChanged() : manager.LastFileHash is not null;
            }
            catch (IOException)
            {
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        private static void RefreshBehaviourBindings(object? content)
        {
            if (content is FrameworkElement { DataContext: NotifyPropertyChangedViewModel viewModel })
            {
                viewModel.OnPropertyChanged(string.Empty);
                if (viewModel is BehaviourViewModel)
                {
                    viewModel.OnPropertyChanged(nameof(BehaviourViewModel.IsRobloxInstallationMissing));
                    viewModel.OnPropertyChanged(nameof(BehaviourViewModel.ForceRobloxReinstallation));
                }
            }
        }

        private void RootFrame_Navigated(object sender, NavigationEventArgs e) => RefreshBehaviourBindings(e.Content);

        #region INavigationWindow methods

        public Frame GetFrame() => RootFrame;

        public INavigation GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(IPageService pageService) => RootNavigation.PageService = pageService;

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        private void ReinstallGate_Changed(object? sender, EventArgs e)
        {
            bool running = ReinstallGate.IsOperationRunning;
            bool finished = _wasReinstallRunning && !running;
            _wasReinstallRunning = running;
            bool generationChanged = ReinstallGate.ObserveGenerationChange();
            if (finished || generationChanged)
            {
                ReinstallGate.RecoveryPending = true;
                _nextRefreshAttempt = DateTime.MinValue;
            }

            if (!running && ReinstallGate.RecoveryPending && DateTime.UtcNow >= _nextRefreshAttempt)
            {
                _nextRefreshAttempt = DateTime.UtcNow.AddSeconds(2);
                try
                {
                    using var readLock = ReinstallOperationLock.TryAcquire(exclusive: false);
                    if (readLock is not null && !ReinstallGate.IsOperationRunning &&
                        VexstrapViewModel.RefreshStatesAfterReinstall(showWarning: finished) &&
                        !ReinstallGate.HasUnseenGeneration())
                    {
                        ReinstallGate.RecoveryPending = false;
                        RefreshBehaviourBindings(RootFrame.Content);
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException("MainWindow::ReinstallGate_Changed", ex);
                }
            }
            bool enabled = !running && !ReinstallGate.RecoveryPending;

            RootGrid.IsEnabled = enabled;
            TestModeToggle.IsEnabled = enabled;
            SaveButton.IsEnabled = enabled;
            SettingsCloseButton.IsEnabled = !running;
            ReinstallStatusText.Text = running ? Strings.Menu_Settings_ReinstallRunning : Strings.Menu_Settings_RefreshPending;
            ReinstallStatusText.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible;
        }

        private void WpfUiWindow_Closing(object sender, CancelEventArgs e)
        {
            // The reinstall can finish just before the next timer tick. Refresh the
            // state before the close handler writes window placement back to disk.
            if ((_wasReinstallRunning || ReinstallGate.IsRunning) && !ReinstallGate.IsOperationRunning)
                ReinstallGate_Changed(this, EventArgs.Empty);

            if (ReinstallGate.RecoveryPending && !ReinstallGate.IsOperationRunning)
            {
                if (Frontend.ShowMessageBox(Strings.Menu_Settings_RefreshClose, MessageBoxImage.Warning, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    e.Cancel = true;
                return; // Do not overwrite unreadable state with stale values.
            }

            using var stateLock = ReinstallOperationLock.TryStart(reinstall: false);
            if (stateLock is null || ReinstallGate.IsRunning)
            {
                e.Cancel = true;
                Frontend.ShowMessageBox(Strings.Menu_Settings_ReinstallRunning, MessageBoxImage.Warning);
                return;
            }

            if (App.FastFlags.Changed || App.PendingSettingTasks.Any())
            {
                var result = Frontend.ShowMessageBox(Strings.Menu_UnsavedChanges, MessageBoxImage.Warning, MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }
            
            _state.Width = this.Width;
            _state.Height = this.Height;

            _state.Top = this.Top;
            _state.Left = this.Left;

            App.State.Save();
        }

        private void WpfUiWindow_Closed(object sender, EventArgs e)
        {
            ReinstallGate.Changed -= ReinstallGate_Changed;
            RootFrame.Navigated -= RootFrame_Navigated;
            _reinstallTimer.Stop();
            _reinstallTimer.Tick -= ReinstallGate_Changed;

            if (App.LaunchSettings.TestModeFlag.Active)
                LaunchHandler.LaunchRoblox(LaunchMode.Player);
            else
                App.SoftTerminate();
        }
    }
}

