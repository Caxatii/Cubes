using UnityEngine;

namespace Cubes.Presentation.Configuration
{
    [CreateAssetMenu(fileName = "GestureConfiguration", menuName = "Cubes/Configuration/Gesture")]
    public sealed class GestureConfigurationAsset : ScriptableObject
    {
        [SerializeField, Min(1f), Tooltip("Movement threshold measured in Canvas units.")] private float _dragThreshold = 10f;

        public float DragThreshold => _dragThreshold;

        public void Validate()
        {
            ConfigurationRange.Require(_dragThreshold, 1f, float.MaxValue, nameof(_dragThreshold));
        }
    }
}
