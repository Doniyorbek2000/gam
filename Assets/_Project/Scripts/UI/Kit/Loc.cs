using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace BilliardGame.UI
{
    /// <summary>
    /// All UI text goes through Unity Localization (string table "UI", locales uz / ru / en). Source of the table is
    /// Assets/_Project/Localization/strings.psv, turned into table assets by BilliardGame ▸ UI ▸ Build Localization.
    /// </summary>
    public static class Loc
    {
        public const string Table = "UI";
        public static readonly string[] Languages = { "uz", "ru", "en" };
        private const string LanguageKey = "language";

        /// <summary>Raised after the language changes; <see cref="LocalizedText"/> refreshes itself on it.</summary>
        public static event Action Changed;

        private static bool hooked;

        public static bool IsReady => LocalizationSettings.InitializationOperation.IsDone;

        public static string Language
        {
            get
            {
                Locale locale = IsReady ? LocalizationSettings.SelectedLocale : null;
                return locale != null ? locale.Identifier.Code : PlayerPrefs.GetString(LanguageKey, DefaultLanguage());
            }
        }

        /// <summary>Blocks until tables are loaded (fine on Android/desktop; the splash calls it once while showing progress).</summary>
        public static void EnsureReady()
        {
            if (!IsReady) LocalizationSettings.InitializationOperation.WaitForCompletion();
            Hook();
            // No startup selector matched (fresh settings asset): pick ours before any label asks for text.
            if (LocalizationSettings.SelectedLocale == null)
            {
                Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(PlayerPrefs.GetString(LanguageKey, DefaultLanguage())));
                if (locale != null) LocalizationSettings.SelectedLocale = locale;
            }
        }

        /// <summary>Picks the saved language, else the device language when we have it, else Uzbek.</summary>
        public static void ApplySavedLanguage() => SetLanguage(PlayerPrefs.GetString(LanguageKey, DefaultLanguage()));

        public static void SetLanguage(string code)
        {
            EnsureReady();
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
            if (locale == null) return;
            PlayerPrefs.SetString(LanguageKey, code);
            if (LocalizationSettings.SelectedLocale != locale) LocalizationSettings.SelectedLocale = locale;
            else Changed?.Invoke();
        }

        public static string Get(string key) => Get(key, null);

        /// <summary>The translation of <paramref name="key"/>, or <paramref name="fallback"/> (default: the key) if missing.</summary>
        public static string Get(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            EnsureReady();
            var table = LocalizationSettings.StringDatabase.GetTable(Table);
            var entry = table != null ? table.GetEntry(key) : null;
            string value = entry?.GetLocalizedString();
            return string.IsNullOrEmpty(value) ? fallback ?? key : value;
        }

        public static string Format(string key, params object[] args)
        {
            string pattern = Get(key);
            try
            {
                return string.Format(Culture, pattern, args);
            }
            catch (FormatException)
            {
                Debug.LogWarning($"[Loc] Bad format for '{key}' in {Language}: {pattern}");
                return pattern;
            }
        }

        /// <summary>Thousands grouped with a thin space, the way all three languages read it.</summary>
        public static string Number(long value) => value.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ");

        public static CultureInfo Culture => CultureInfo.InvariantCulture;

        private static void Hook()
        {
            if (hooked) return;
            hooked = true;
            LocalizationSettings.SelectedLocaleChanged += _ => Changed?.Invoke();
        }

        private static string DefaultLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Russian:
                case SystemLanguage.Belarusian:
                case SystemLanguage.Ukrainian:
                    return "ru";
                case SystemLanguage.English:
                    return "en";
                default:
                    return "uz";
            }
        }
    }
}
