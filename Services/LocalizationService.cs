using System.Globalization;
using System.Resources;
using System.Reflection;

namespace BackupCleaner.Services
{
    /// <summary>
    /// Service voor het beheren van meertaligheid in de applicatie.
    /// </summary>
    public static class LocalizationService
    {
        private static ResourceManager? _resourceManager;
        private static CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

        /// <summary>
        /// Beschikbare taalcodes
        /// </summary>
        public const string LanguageAuto = "auto";
        public const string LanguageEnglish = "en";
        public const string LanguageDutch = "nl";

        /// <summary>
        /// Initialiseert de localization service met de opgegeven taalcode.
        /// </summary>
        /// <param name="languageCode">Taalcode (auto, en, nl)</param>
        public static void Initialize(string languageCode)
        {
            _resourceManager = new ResourceManager("BackupCleaner.Resources.Strings", Assembly.GetExecutingAssembly());
            SetLanguage(languageCode);
        }

        /// <summary>
        /// Stelt de huidige taal in op basis van de opgegeven taalcode.
        /// </summary>
        /// <param name="languageCode">Taalcode (auto, en, nl)</param>
        public static void SetLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode) || languageCode == LanguageAuto)
            {
                // Detecteer systeemtaal
                var systemCulture = CultureInfo.CurrentUICulture;
                
                // Als de systeemtaal Nederlands is, gebruik Nederlands, anders Engels
                if (systemCulture.TwoLetterISOLanguageName == "nl")
                {
                    _currentCulture = new CultureInfo("nl");
                }
                else
                {
                    _currentCulture = new CultureInfo("en");
                }
            }
            else
            {
                _currentCulture = new CultureInfo(languageCode);
            }

            // Update de huidige thread culture
            CultureInfo.CurrentUICulture = _currentCulture;
        }

        /// <summary>
        /// Haalt de huidige taalcode op.
        /// </summary>
        public static string CurrentLanguageCode => _currentCulture.TwoLetterISOLanguageName;

        /// <summary>
        /// Haalt een vertaalde string op uit de resources.
        /// </summary>
        /// <param name="key">De resource key</param>
        /// <returns>De vertaalde string of de key als fallback</returns>
        public static string GetString(string key)
        {
            if (_resourceManager == null)
            {
                Initialize(LanguageAuto);
            }

            try
            {
                var value = _resourceManager?.GetString(key, _currentCulture);
                return value ?? key;
            }
            catch
            {
                return key;
            }
        }

        /// <summary>
        /// Haalt een vertaalde string op en formatteert deze met de opgegeven argumenten.
        /// </summary>
        /// <param name="key">De resource key</param>
        /// <param name="args">Format argumenten</param>
        /// <returns>De geformatteerde, vertaalde string</returns>
        public static string GetString(string key, params object[] args)
        {
            var format = GetString(key);
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }

        /// <summary>
        /// Bepaalt de systeemtaal en retourneert de bijbehorende taalcode.
        /// </summary>
        public static string DetectSystemLanguage()
        {
            var systemCulture = CultureInfo.CurrentUICulture;
            return systemCulture.TwoLetterISOLanguageName == "nl" ? LanguageDutch : LanguageEnglish;
        }

        /// <summary>
        /// Controleert of de huidige taal Nederlands is.
        /// </summary>
        public static bool IsDutch => _currentCulture.TwoLetterISOLanguageName == "nl";

        /// <summary>
        /// Controleert of de huidige taal Engels is.
        /// </summary>
        public static bool IsEnglish => _currentCulture.TwoLetterISOLanguageName == "en";
    }
}


