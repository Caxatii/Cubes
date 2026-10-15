using System;
using System.Collections.Generic;

namespace Cubes.Application.Localization
{
    public interface IGameLocalization
    {
        string Language { get; }
        IReadOnlyList<string> AvailableLanguages { get; }
        event Action Changed;
        string Get(string key);
        void SetLanguage(string language);
    }
}
