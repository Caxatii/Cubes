using System;
using UnityEngine;

namespace Cubes.Presentation.Configuration
{
    [CreateAssetMenu(fileName = "NotificationConfiguration", menuName = "Cubes/Configuration/Notification")]
    public sealed class NotificationConfigurationAsset : ScriptableObject
    {
        [SerializeField, Min(1)] private int _maximumNotificationCount = 3;
        [SerializeField, Min(0.01f)] private float _notificationDisplayDuration = 2f;
        [SerializeField, Min(0f)] private float _notificationSpacing = 12f;
        [SerializeField, Min(0f)] private float _notificationEnterDuration = 0.2f;
        [SerializeField, Min(0f)] private float _notificationShiftDuration = 0.2f;
        [SerializeField, Min(0f)] private float _notificationExitDuration = 0.2f;

        public int MaximumCount => _maximumNotificationCount;
        public float DisplayDuration => _notificationDisplayDuration;
        public float Spacing => _notificationSpacing;
        public float EnterDuration => _notificationEnterDuration;
        public float ShiftDuration => _notificationShiftDuration;
        public float ExitDuration => _notificationExitDuration;

        public void Validate()
        {
            if (_maximumNotificationCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(_maximumNotificationCount));
            }

            ConfigurationRange.Require(_notificationDisplayDuration, 0.01f, float.MaxValue, nameof(_notificationDisplayDuration));
            ConfigurationRange.Require(_notificationSpacing, 0f, float.MaxValue, nameof(_notificationSpacing));
            ConfigurationRange.Require(_notificationEnterDuration, 0f, float.MaxValue, nameof(_notificationEnterDuration));
            ConfigurationRange.Require(_notificationShiftDuration, 0f, float.MaxValue, nameof(_notificationShiftDuration));
            ConfigurationRange.Require(_notificationExitDuration, 0f, float.MaxValue, nameof(_notificationExitDuration));
        }
    }
}
