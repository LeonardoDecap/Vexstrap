using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;
using ICSharpCode.SharpZipLib.Zip;

using Microsoft.Win32;

using Vexstrap.UI.Elements.Settings;
using Vexstrap.UI.Elements.Editor;
using Vexstrap.UI.Elements.Dialogs;

namespace Vexstrap.UI.ViewModels.Settings
{
    public class AppearanceViewModel : NotifyPropertyChangedViewModel
    {
        private readonly Page _page;

        public ICommand PreviewBootstrapperCommand => new RelayCommand(PreviewBootstrapper);
        public ICommand BrowseCustomIconLocationCommand => new RelayCommand(BrowseCustomIconLocation);

        public ICommand AddCustomThemeCommand => new RelayCommand(AddCustomTheme);
        public ICommand DeleteCustomThemeCommand => new RelayCommand(DeleteCustomTheme);
        public ICommand RenameCustomThemeCommand => new RelayCommand(RenameCustomTheme);
        public ICommand EditCustomThemeCommand => new RelayCommand(EditCustomTheme);
        public ICommand ExportCustomThemeCommand => new RelayCommand(ExportCustomTheme);

        private void PreviewBootstrapper()
        {
            IBootstrapperDialog dialog = App.Settings.Prop.BootstrapperStyle.GetNew();

            if (App.Settings.Prop.BootstrapperStyle == BootstrapperStyle.ByfronDialog)
                dialog.Message = Strings.Bootstrapper_StylePreview_ImageCancel;
            else
                dialog.Message = Strings.Bootstrapper_StylePreview_TextCancel;

            dialog.CancelEnabled = true;
            dialog.ShowBootstrapper();
        }

        private void BrowseCustomIconLocation()
        {
            var dialog = new OpenFileDialog
            {
                Filter = $"{Strings.Menu_IconFiles}|*.ico"
            };

            if (dialog.ShowDialog() != true)
                return;

            CustomIconLocation = dialog.FileName;
            OnPropertyChanged(nameof(CustomIconLocation));
        }

        
        private void UpdateCustomIconState()
        {
            foreach (var icon in Icons)
            {
                if (icon.IconType == BootstrapperIcon.IconCustom)
                {
                    icon.IsEnabled = !string.IsNullOrEmpty(CustomIconLocation) && System.IO.File.Exists(CustomIconLocation);
                    icon.OnPropertyChanged("ImageSource");
                    break;
                }
            }
        }

        public AppearanceViewModel(Page page)
        {
            _page = page;

            foreach (var entry in BootstrapperIconEx.Selections)
            {
                bool isEnabled = true;
                if (entry == BootstrapperIcon.IconCustom)
                {
                    isEnabled = !string.IsNullOrEmpty(App.Settings.Prop.BootstrapperIconCustomLocation) && System.IO.File.Exists(App.Settings.Prop.BootstrapperIconCustomLocation);
                }
                Icons.Add(new BootstrapperIconEntry { IconType = entry, IsSelected = (entry == App.Settings.Prop.BootstrapperIcon), IsEnabled = isEnabled });
            }

            PopulateCustomThemes();
        }

        public IEnumerable<Theme> Themes { get; } = Enum.GetValues(typeof(Theme)).Cast<Theme>();

        public Theme Theme
        {
            get => App.Settings.Prop.Theme;
            set
            {
                App.Settings.Prop.Theme = value;
                ((MainWindow)Window.GetWindow(_page)!).ApplyTheme();
            }
        }

        public ObservableCollection<ThemePresetEntry> ThemePresetEntries { get; } = BuildThemePresetEntries();

        
        public System.Windows.Input.ICommand SelectIconCommand => new RelayCommand<Models.BootstrapperIconEntry>(SelectIcon);

        private void SelectIcon(Models.BootstrapperIconEntry? entry)
        {
            if (entry == null) return;
            Icon = entry.IconType; // Set the property, it handles the rest
        }

        public ICommand SelectThemePresetCommand => new RelayCommand<string>(SelectThemePreset);

        private static ObservableCollection<ThemePresetEntry> BuildThemePresetEntries()
        {
            string current = App.Settings.Prop.ThemePreset ?? "";

            var list = new ObservableCollection<ThemePresetEntry>
            {
                new() { Id = "", Name = "System", Swatch = System.Windows.Media.Brushes.Gray, IsSelected = current == "" }
            };

            foreach (var p in ThemePresets.All)
                list.Add(new() { Id = p.Id, Name = p.Name, Swatch = new System.Windows.Media.SolidColorBrush(p.Accent), IsSelected = current == p.Id });

            return list;
        }

        private void SelectThemePreset(string? id)
        {
            var preset = ThemePresets.Find(id);

            App.Settings.Prop.ThemePreset = preset?.Id ?? "";
            App.Settings.Prop.Theme = preset is null
                ? Vexstrap.Enums.Theme.Default
                : (preset.Dark ? Vexstrap.Enums.Theme.Dark : Vexstrap.Enums.Theme.Light);

            foreach (var entry in ThemePresetEntries)
                entry.IsSelected = entry.Id == App.Settings.Prop.ThemePreset;

            OnPropertyChanged(nameof(Theme));
            ((MainWindow)Window.GetWindow(_page)!).ApplyTheme();
        }

        public static List<string> Languages => Locale.GetLanguages();

        public string SelectedLanguage 
        { 
            get => Locale.SupportedLocales[App.Settings.Prop.Locale]; 
            set => App.Settings.Prop.Locale = Locale.GetIdentifierFromName(value);
        }

        public IEnumerable<BootstrapperStyle> Dialogs { get; } = BootstrapperStyleEx.Selections;

        public BootstrapperStyle Dialog
        {
            get => App.Settings.Prop.BootstrapperStyle;
            set
            {
                App.Settings.Prop.BootstrapperStyle = value;
                OnPropertyChanged(nameof(CustomThemesExpanded)); // TODO: only fire when needed
            }
        }

        public bool CustomThemesExpanded => App.Settings.Prop.BootstrapperStyle == BootstrapperStyle.CustomDialog;

        public ObservableCollection<BootstrapperIconEntry> Icons { get; set; } = new();

        public System.Collections.Generic.IEnumerable<Models.BootstrapperIconEntry> ClassicIcons => Icons.Where(x => x.IconType == BootstrapperIcon.IconThemeMidnightAmethyst);
        public System.Collections.Generic.IEnumerable<Models.BootstrapperIconEntry> ThemeIcons => Icons.Where(x => x.IconType.ToString().StartsWith("IconTheme") && x.IconType != BootstrapperIcon.IconThemeMidnightAmethyst);
        public System.Collections.Generic.IEnumerable<Models.BootstrapperIconEntry> LegacyIcons => Icons.Where(x => x.IconType.ToString().StartsWith("Icon20") || x.IconType.ToString().Contains("2015"));
        public System.Collections.Generic.IEnumerable<Models.BootstrapperIconEntry> CustomIcons => Icons.Where(x => x.IconType == BootstrapperIcon.IconCustom);


        public BootstrapperIcon Icon
        {
            get => App.Settings.Prop.BootstrapperIcon;
            set 
            {
                App.Settings.Prop.BootstrapperIcon = value;
                OnPropertyChanged(nameof(Icon));
                foreach (var i in Icons)
                    i.IsSelected = (i.IconType == value);
            }
        }

        public string Title
        {
            get => App.Settings.Prop.BootstrapperTitle;
            set => App.Settings.Prop.BootstrapperTitle = value;
        }

        public string CustomIconLocation
        {
            get => App.Settings.Prop.BootstrapperIconCustomLocation;
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    if (App.Settings.Prop.BootstrapperIcon == BootstrapperIcon.IconCustom)
                        App.Settings.Prop.BootstrapperIcon = BootstrapperIcon.IconVexstrap;
                }
                else
                {
                    App.Settings.Prop.BootstrapperIcon = BootstrapperIcon.IconCustom;
                }

                App.Settings.Prop.BootstrapperIconCustomLocation = value;
                UpdateCustomIconState();

                OnPropertyChanged(nameof(Icon));
                OnPropertyChanged(nameof(Icons));
            }
        }

        private void DeleteCustomThemeStructure(string name)
        {
            string dir = Path.Combine(Paths.CustomThemes, name);
            Directory.Delete(dir, true);
        }

        private void RenameCustomThemeStructure(string oldName, string newName)
        {
            string oldDir = Path.Combine(Paths.CustomThemes, oldName);
            string newDir = Path.Combine(Paths.CustomThemes, newName);
            Directory.Move(oldDir, newDir);
        }

        private void AddCustomTheme()
        {
            var dialog = new AddCustomThemeDialog();
            dialog.ShowDialog();

            if (dialog.Created)
            {
                CustomThemes.Add(dialog.ThemeName);
                SelectedCustomThemeIndex = CustomThemes.Count - 1;

                OnPropertyChanged(nameof(SelectedCustomThemeIndex));
                OnPropertyChanged(nameof(IsCustomThemeSelected));

                if (dialog.OpenEditor)
                    EditCustomTheme();
            }
        }

        private void DeleteCustomTheme()
        {
            if (SelectedCustomTheme is null)
                return;

            try
            {
                DeleteCustomThemeStructure(SelectedCustomTheme);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("AppearanceViewModel::DeleteCustomTheme", ex);
                Frontend.ShowMessageBox(string.Format(Strings.Menu_Appearance_CustomThemes_DeleteFailed, SelectedCustomTheme, ex.Message), MessageBoxImage.Error);
                return;
            }

            CustomThemes.Remove(SelectedCustomTheme);

            if (CustomThemes.Any())
            {
                SelectedCustomThemeIndex = CustomThemes.Count - 1;
                OnPropertyChanged(nameof(SelectedCustomThemeIndex));
            }

            OnPropertyChanged(nameof(IsCustomThemeSelected));
        }

        private void RenameCustomTheme()
        {
            const string LOG_IDENT = "AppearanceViewModel::RenameCustomTheme";

            if (SelectedCustomTheme is null || SelectedCustomTheme == SelectedCustomThemeName)
                return;

            if (string.IsNullOrEmpty(SelectedCustomThemeName))
            {
                Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_NameEmpty, MessageBoxImage.Error);
                return;
            }

            var validationResult = PathValidator.IsFileNameValid(SelectedCustomThemeName);

            if (validationResult != PathValidator.ValidationResult.Ok)
            {
                switch (validationResult)
                {
                    case PathValidator.ValidationResult.IllegalCharacter:
                        Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_NameIllegalCharacters, MessageBoxImage.Error);
                        break;
                    case PathValidator.ValidationResult.ReservedFileName:
                        Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_NameReserved, MessageBoxImage.Error);
                        break;
                    default:
                        App.Logger.WriteLine(LOG_IDENT, $"Got unhandled PathValidator::ValidationResult {validationResult}");
                        Debug.Assert(false);

                        Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_Unknown, MessageBoxImage.Error);
                        break;
                }

                return;
            }

            // better to check for the file instead of the directory so broken themes can be overwritten
            string path = Path.Combine(Paths.CustomThemes, SelectedCustomThemeName, "Theme.xml");
            if (File.Exists(path))
            {
                Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_NameTaken, MessageBoxImage.Error);
                return;
            }

            try
            {
                RenameCustomThemeStructure(SelectedCustomTheme, SelectedCustomThemeName);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                Frontend.ShowMessageBox(string.Format(Strings.Menu_Appearance_CustomThemes_RenameFailed, SelectedCustomTheme, ex.Message), MessageBoxImage.Error);
                return;
            }

            int idx = CustomThemes.IndexOf(SelectedCustomTheme);
            CustomThemes[idx] = SelectedCustomThemeName;

            SelectedCustomThemeIndex = idx;
            OnPropertyChanged(nameof(SelectedCustomThemeIndex));
        }

        private void EditCustomTheme()
        {
            if (SelectedCustomTheme is null)
                return;

            new BootstrapperEditorWindow(SelectedCustomTheme).ShowDialog();
        }

        private void ExportCustomTheme()
        {
            if (SelectedCustomTheme is null)
                return;

            var dialog = new SaveFileDialog
            {
                FileName = $"{SelectedCustomTheme}.zip",
                Filter = $"{Strings.FileTypes_ZipArchive}|*.zip"
            };

            if (dialog.ShowDialog() != true)
                return;

            string themeDir = Path.Combine(Paths.CustomThemes, SelectedCustomTheme);

            using var memStream = new MemoryStream();
            using var zipStream = new ZipOutputStream(memStream);

            foreach (var filePath in Directory.EnumerateFiles(themeDir, "*.*", SearchOption.AllDirectories))
            {
                string relativePath = filePath[(themeDir.Length + 1)..];

                var entry = new ZipEntry(relativePath);
                entry.DateTime = DateTime.Now;

                zipStream.PutNextEntry(entry);

                using var fileStream = File.OpenRead(filePath);
                fileStream.CopyTo(zipStream);
            }

            zipStream.CloseEntry();
            zipStream.Finish();
            memStream.Position = 0;

            using var outputStream = File.OpenWrite(dialog.FileName);
            memStream.CopyTo(outputStream);

            Process.Start("explorer.exe", $"/select,\"{dialog.FileName}\"");
        }

        private void PopulateCustomThemes()
        {
            string? selected = App.Settings.Prop.SelectedCustomTheme;

            Directory.CreateDirectory(Paths.CustomThemes);

            foreach (string directory in Directory.GetDirectories(Paths.CustomThemes))
            {
                if (!File.Exists(Path.Combine(directory, "Theme.xml")))
                    continue; // missing the main theme file, ignore

                string name = Path.GetFileName(directory);
                CustomThemes.Add(name);
            }

            if (selected != null)
            {
                int idx = CustomThemes.IndexOf(selected);

                if (idx != -1)
                {
                    SelectedCustomThemeIndex = idx;
                    OnPropertyChanged(nameof(SelectedCustomThemeIndex));
                }
                else
                {
                    SelectedCustomTheme = null;
                }
            }
        }

        public string? SelectedCustomTheme
        {
            get => App.Settings.Prop.SelectedCustomTheme;
            set => App.Settings.Prop.SelectedCustomTheme = value;
        }

        public string SelectedCustomThemeName { get; set; } = "";

        public int SelectedCustomThemeIndex { get; set; }

        public ObservableCollection<string> CustomThemes { get; set; } = new();
        public bool IsCustomThemeSelected => SelectedCustomTheme is not null;
    }
}

