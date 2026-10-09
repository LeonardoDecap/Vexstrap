using System;
using System.Threading.Tasks;
using System.Windows;

namespace Vexstrap.UI.Elements.Dialogs
{
    public partial class FastFlagWarningDialog : Vexstrap.UI.Elements.Base.WpfUiWindow
    {
        private int _countdown = 8;
        public bool Accepted { get; private set; } = false;

        public FastFlagWarningDialog()
        {
            InitializeComponent();
            StartCountdown();
        }

        private async void StartCountdown()
        {
            while (_countdown > 0)
            {
                AcceptButton.Content = $"Wait ({_countdown})";
                await Task.Delay(1000);
                _countdown--;
            }
            
            AcceptButton.Content = "I Understand";
            
            AcceptButton.IsEnabled = true;
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            Accepted = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Accepted = false;
            this.Close();
        }
    }
}
