using System.Windows;

namespace Vexstrap
{
    internal static class Locale
    {
        public static CultureInfo CurrentCulture { get; private set; } = CultureInfo.InvariantCulture;

        public static bool RightToLeft { get; private set; } = false;

        private static readonly List<string> _rtlLocales = new() { "ar", "he", "fa" };

        public static readonly Dictionary<string, string> SupportedLocales = new()
        {
            { "nil", Strings.Common_SystemDefault },
            { "en", "English" },
            { "en-US", "English (United States)" },
#if QA_BUILD
            { "sq", "Albanian" }, // Albanian (TODO: translate string)
#endif
            { "ar", "???????" }, // Arabic
            { "bg", "?????????" }, // Bulgarian
#if QA_BUILD
            { "bn", "?????" }, // Bengali
            { "bs", "Bosanski" }, // Bosnian
#endif
            { "cs", "Ceština" }, // Czech
            { "de", "Deutsch" }, // German
            { "da", "Dansk" }, // Danish
            { "es-ES", "Español" }, // Spanish
#if QA_BUILD
            { "el", "????????" }, // Greek
#endif
            { "fa", "?????" }, // Persian
            { "fi", "Suomi" }, // Finnish
            { "fil", "Filipino" }, // Filipino
            { "fr", "Français" }, // French
#if QA_BUILD
            { "he", "??????" }, // Hebrew
#endif
            { "hi", "Hindi (Latin)" }, // Hindi
            { "hr", "Hrvatski" }, // Croatian
#if QA_BUILD
            { "hu", "Magyar" }, // Hungarian
            { "is", "Íslenska" }, // Icelandic
#endif
            { "id", "Bahasa Indonesia" }, // Indonesian
            { "it", "Italiano" }, // Italian
            { "ja", "???" }, // Japanese
            { "ko", "???" }, // Korean
            { "lv", "Latviešu" }, // Latvian
            { "lt", "Lietuviu" }, // Lithuanian
#if QA_BUILD
            { "ms", "Malay" }, // Malay
#endif
            { "nl", "Nederlands" }, // Dutch
#if QA_BUILD
            { "et", "Eesti Keel" }, // Estonian
            { "no", "Bokmål" }, // Norwegian
#endif
            { "pl", "Polski" }, // Polish
#if QA_BUILD
            { "pt-PT", "Portugese (European)" }, // Portuguese (TODO: translate)
#endif
            { "pt-BR", "Português (Brasil)" }, // Portuguese, Brazilian
            { "ro", "Româna" }, // Romanian
            { "ru", "???????" }, // Russian
#if QA_BUILD
            { "sr-CS", "Serbian (Latin)" }, // Serbian (TODO: translate)
#endif
            { "sv-SE", "Svenska" }, // Swedish
            { "th", "???????" }, // Thai
            { "tr", "Türkçe" }, // Turkish
#if QA_BUILD
            { "uk", "??????????" }, // Ukrainian
#endif
            { "vi", "Ti?ng Vi?t" }, // Vietnamese
            { "zh-Hans-CN", "?? (??)" }, // Chinese Simplified
            { "zh-Hant-HK", "?? (??)" }, // Chinese Traditional, Hong Kong
            { "zh-Hant-TW", "?? (??)" } // Chinese Traditional
        };

        public static string GetIdentifierFromName(string language) => SupportedLocales.FirstOrDefault(x => x.Value == language).Key ?? "nil";

        public static List<string> GetLanguages()
        {
            var languages = new List<string>();
            
            languages.AddRange(SupportedLocales.Values.Take(3));
            languages.AddRange(SupportedLocales.Values.Where(x => !languages.Contains(x)).OrderBy(x => x));
            languages[0] = Strings.Common_SystemDefault; // set again for any locale changes

            return languages;
        }

        public static void Set(string identifier)
        {
            if (!SupportedLocales.ContainsKey(identifier))
                identifier = "nil";

            if (identifier == "nil")
            {
                CurrentCulture = Thread.CurrentThread.CurrentUICulture;
            }
            else
            {
                CurrentCulture = new CultureInfo(identifier);

                CultureInfo.DefaultThreadCurrentUICulture = CurrentCulture;
                Thread.CurrentThread.CurrentUICulture = CurrentCulture;
            }

            RightToLeft = _rtlLocales.Any(CurrentCulture.Name.StartsWith);
        }

        public static void Initialize()
        {
            Set("nil");

            // https://supportcenter.devexpress.com/ticket/details/t905790/is-there-a-way-to-set-right-to-left-mode-in-wpf-for-the-whole-application
            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler((sender, _) =>
            {
                var window = (Window)sender;

                if (RightToLeft)
                {
                    window.FlowDirection = FlowDirection.RightToLeft;

                    if (window.ContextMenu is not null)
                        window.ContextMenu.FlowDirection = FlowDirection.RightToLeft;
                }
                else if (CurrentCulture.Name.StartsWith("th"))
                {
                    window.FontFamily = new System.Windows.Media.FontFamily(new Uri("pack://application:,,,/Resources/Fonts/"), "./#Noto Sans Thai");
                }

#if QA_BUILD
                window.BorderBrush = System.Windows.Media.Brushes.Red;
                window.BorderThickness = new Thickness(4);
#endif
            }));
        }
    }
}

