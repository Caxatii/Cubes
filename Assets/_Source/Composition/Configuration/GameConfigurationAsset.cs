using System;
using Cubes.Application.Configuration;
using Cubes.Infrastructure.Configuration;
using Cubes.Presentation.Configuration;
using UnityEngine;

namespace Cubes.Composition.Configuration
{
    [CreateAssetMenu(fileName = "GameConfiguration", menuName = "Cubes/Configuration/Game")]
    public sealed class GameConfigurationAsset : ScriptableObject, IGameConfigurationSource
    {
        [SerializeField] private CubeBarAppearanceAsset _cubeBar;
        [SerializeField] private TowerConfigurationAsset _tower;
        [SerializeField] private GestureConfigurationAsset _gestures;
        [SerializeField] private AnimationConfigurationAsset _animations;
        [SerializeField] private NotificationConfigurationAsset _notifications;

        public CubeBarAppearanceAsset CubeBar => _cubeBar;
        public TowerConfigurationAsset Tower => _tower;
        public GestureConfigurationAsset Gestures => _gestures;
        public AnimationConfigurationAsset Animations => _animations;
        public NotificationConfigurationAsset Notifications => _notifications;

        public GameConfiguration CreateConfiguration()
        {
            ValidatePresentation();

            if (_tower == null)
                throw new InvalidOperationException("Tower configuration must be assigned.");

            return new GameConfiguration(_cubeBar.GetColors(),
                _tower.MaximumHorizontalStep, _tower.MaximumBaseOffset, _tower.PlayfieldWidth, _tower.PlayfieldHeight);
        }

        public void ValidatePresentation()
        {
            if (_cubeBar == null || _gestures == null
                || _animations == null || _notifications == null)
            {
                throw new InvalidOperationException("Every game configuration section must be assigned.");
            }

            _gestures.Validate();
            _animations.Validate();
            _notifications.Validate();
            _cubeBar.Validate();

        }
    }
}
