using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Vexstrap.UI.ViewModels.Dialogs;
using Vexstrap.UI.ViewModels.Installer;
using Wpf.Ui.Mvvm.Interfaces;

namespace Vexstrap.UI.Elements.Dialogs
{
    /// <summary>
    /// Interaction logic for LaunchMenuDialog.xaml
    /// </summary>
    public partial class LaunchMenuDialog
    {
        public NextAction CloseAction = NextAction.Terminate;

        
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Vexstrap.Utility.TaskbarIcon.Apply(this, Vexstrap.Utility.TaskbarIcon.GetCurrentIconPath());
        }

        public LaunchMenuDialog()
        {
            var viewModel = new LaunchMenuViewModel();
            viewModel.CloseWindowRequest += (_, closeAction) =>
            {
                CloseAction = closeAction;
                Close();
            };

            DataContext = viewModel;

            InitializeComponent();
        }

        private void LaunchRoblox_Click(object sender, RoutedEventArgs e)
        {
            CloseAction = NextAction.LaunchRoblox;
            Close();
        }

        private void LaunchRobloxStudio_Click(object sender, RoutedEventArgs e)
        {
            CloseAction = NextAction.LaunchRobloxStudio;
            Close();
        }

        private void LaunchSettings_Click(object sender, RoutedEventArgs e)
        {
            CloseAction = NextAction.LaunchSettings;
            Close();
        }

    }
}

