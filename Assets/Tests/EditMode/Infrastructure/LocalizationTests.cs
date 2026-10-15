using System.Collections.Generic;
using Cubes.Application.Localization;
using Cubes.Presentation.Gameplay;
using Cubes.Infrastructure.Localization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Cubes.Tests.EditMode.Infrastructure
{
    public sealed class LocalizationTests
    {
        private Locale _previousLocale;
        private GameLocalization _localization;

        [SetUp]
        public void SetUp()
        {
            LocalizationSettings.InitializationOperation.WaitForCompletion();
            _previousLocale = LocalizationSettings.SelectedLocale;
            _localization = new GameLocalization();
        }

        [TearDown]
        public void TearDown()
        {
            _localization.Dispose();
            LocalizationSettings.SelectedLocale = _previousLocale;
        }

        [Test]
        public void TranslationFollowsTheGlobalLocale()
        {
            _localization.SetLanguage("ru");
            string russian = _localization.Get(LocalizationKeys.ResetButton);
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
            string english = _localization.Get(LocalizationKeys.ResetButton);

            Assert.That(_localization.Language, Is.EqualTo("en"));
            Assert.That(russian, Is.Not.Empty.And.Not.EqualTo(english));
            Assert.That(russian, Does.Not.Contain("No translation found"));
            Assert.That(english, Does.Not.Contain("No translation found"));
        }

        [Test]
        public void AddedLocaleParticipatesInSwitchingWithoutCodeChanges()
        {
            Locale additional = Locale.CreateLocale("ja");
            LocalizationSettings.AvailableLocales.AddLocale(additional);
            try
            {
                HashSet<string> visited = new HashSet<string>();
                int count = LocalizationSettings.AvailableLocales.Locales.Count;
                for (int index = 0; index < count; index++)
                {
                    _localization.SetLanguage(_localization.AvailableLanguages[index]);
                    visited.Add(_localization.Language);
                }

                Assert.That(visited.Count, Is.EqualTo(count));
                Assert.That(visited, Does.Contain("ja"));
            }
            finally
            {
                LocalizationSettings.SelectedLocale = _previousLocale;
                LocalizationSettings.AvailableLocales.RemoveLocale(additional);
                Object.DestroyImmediate(additional);
            }
        }

        [Test]
        public void UnavailableSavedLocalePreservesTheConfiguredFallback()
        {
            _localization.SetLanguage("en");
            Assert.Throws<System.ArgumentException>(() => _localization.SetLanguage("zz"));
            Assert.That(_localization.Language, Is.EqualTo("en"));
        }
    }
}
