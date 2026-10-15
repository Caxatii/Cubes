using System;
using System.Collections.Generic;
using Cubes.Application.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Cubes.Infrastructure.Localization
{
    public sealed class GameLocalization : IGameLocalization, IDisposable
    {
        private const string TableName = "Game";

        public GameLocalization()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        public string Language => LocalizationSettings.SelectedLocale.Identifier.Code;

        public IReadOnlyList<string> AvailableLanguages
        {
            get
            {
                List<string> languages = new List<string>();

                foreach (Locale locale in LocalizationSettings.AvailableLocales.Locales)
                {
                    languages.Add(locale.Identifier.Code);
                }

                return languages.AsReadOnly();
            }
        }

        public event Action Changed;

        public string Get(string key) => LocalizationSettings.StringDatabase.GetLocalizedString(TableName, key);

        public void SetLanguage(string language)
        {
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(language);
            if (locale == null)
            {
                throw new ArgumentException("Unknown game language.", nameof(language));
            }

            LocalizationSettings.SelectedLocale = locale;
        }

        public void Dispose() => LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

        private void OnLocaleChanged(Locale locale) => Changed?.Invoke();
    }
}
