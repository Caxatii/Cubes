using Cubes.Application.Gameplay;
using Cubes.Application.Configuration;
using Cubes.Application.Localization;
using Cubes.Application.Progress;
using Cubes.Composition.Configuration;
using Cubes.Presentation.Gameplay;
using VContainer.Unity;
using UnityEngine.Localization.Settings;

namespace Cubes.Composition.Gameplay
{
    public sealed class GameBootstrap : IStartable
    {
        private readonly GameConfigurationAsset _assets;
        private readonly GameplayScreen _screen;
        private readonly IGameLocalization _localization;
        private readonly GameSession _session;
        private readonly GameplayPresenter _presenter;
        private readonly ProgressService _progress;
        private readonly GameConfiguration _configuration;

        public GameBootstrap(GameConfigurationAsset assets, GameplayScreen screen,
            IGameLocalization localization, GameSession session, GameplayPresenter presenter, ProgressService progress,
            GameConfiguration configuration)
        {
            _assets = assets;
            _screen = screen;
            _localization = localization;
            _session = session;
            _presenter = presenter;
            _progress = progress;
            _configuration = configuration;
        }

        public void Start()
        {
            _assets.ValidatePresentation();
            _assets.CubeBar.ValidateCatalog(_configuration);
            LocalizationSettings.InitializationOperation.WaitForCompletion();
            _progress.TryLoad(_session, _localization, out string diagnostic);

            _screen.Initialize(_assets.CubeBar, _session, _assets.Animations,
                _assets.Gestures, _assets.Notifications, _localization);

            _presenter.Initialize(diagnostic);
        }
    }
}
