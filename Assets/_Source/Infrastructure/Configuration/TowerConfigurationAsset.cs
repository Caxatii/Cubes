using UnityEngine;

namespace Cubes.Infrastructure.Configuration
{
    [CreateAssetMenu(fileName = "TowerConfiguration", menuName = "Cubes/Configuration/Tower")]
    public sealed class TowerConfigurationAsset : ScriptableObject
    {
        [SerializeField, Range(0f, 0.5f)] private float _maximumHorizontalStep = 0.5f;
        [SerializeField, Range(0f, 0.49f)] private float _maximumBaseOffset = 0.4f;
        [SerializeField, Min(1f)] private float _playfieldWidth = 7.07777786f;
        [SerializeField, Min(1f)] private float _playfieldHeight = 6.467762f;

        public float MaximumHorizontalStep => _maximumHorizontalStep;
        public float MaximumBaseOffset => _maximumBaseOffset;
        public float PlayfieldWidth => _playfieldWidth;
        public float PlayfieldHeight => _playfieldHeight;
    }
}
