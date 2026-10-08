using System.Windows.Media;

namespace Vexstrap.Models
{
    public class BootstrapperIconEntry : Vexstrap.UI.ViewModels.NotifyPropertyChangedViewModel
    {
        public Enums.BootstrapperIcon IconType { get; set; }
        
        // safe image source access
        public ImageSource ImageSource 
        {
            get 
            {
                try 
                {
                    return Vexstrap.Extensions.BootstrapperIconEx.GetIcon(IconType).GetImageSource();
                }
                catch (System.Exception ex)
                {
                    App.Logger.WriteLine("BootstrapperIconEntry", $"Failed to load image source for {IconType}: {ex.Message}");
                    return Vexstrap.Extensions.BootstrapperIconEx.GetIcon(Enums.BootstrapperIcon.IconVexstrap).GetImageSource();
                }
            }
        }

        private bool _isSelected;
        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }
    }
}
