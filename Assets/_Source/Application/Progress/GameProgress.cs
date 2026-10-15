using System;
using Cubes.Application.Gameplay;

namespace Cubes.Application.Progress
{
    public sealed class GameProgress
    {
        public const int CurrentFormatVersion = 1;
        public int FormatVersion { get; }

        public GameProgress(string language, TowerState tower)
        {
            FormatVersion = CurrentFormatVersion;
            Language = language;
            Tower = tower ?? throw new ArgumentNullException(nameof(tower));
        }

        public string Language { get; }
        public TowerState Tower { get; }
    }
}
