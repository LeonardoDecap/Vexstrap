namespace Vexstrap.UI.ViewModels.Settings
{
    public class ThemePresetEntry : NotifyPropertyChangedViewModel
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public System.Windows.Media.Brush Swatch { get; init; } = System.Windows.Media.Brushes.Gray;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }
    }
}