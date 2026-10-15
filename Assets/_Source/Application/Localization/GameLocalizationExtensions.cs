using System;
using System.Collections.Generic;

namespace Cubes.Application.Localization
{
    public static class GameLocalizationExtensions
    {
        public static string GetNextLanguage(this IGameLocalization localization)
        {
            IReadOnlyList<string> languages = localization.AvailableLanguages;
            if (languages.Count == 0)
            {
                throw new InvalidOperationException("No game languages are configured.");
            }

            for (int index = 0; index < languages.Count; index++)
            {
                if (languages[index] == localization.Language)
                {
                    return languages[(index + 1) % languages.Count];
                }
            }

            return languages[0];
        }
    }
}
