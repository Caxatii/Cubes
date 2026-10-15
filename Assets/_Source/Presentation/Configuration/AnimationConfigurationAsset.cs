using UnityEngine;

namespace Cubes.Presentation.Configuration
{
    [CreateAssetMenu(fileName = "AnimationConfiguration", menuName = "Cubes/Configuration/Animation")]
    public sealed class AnimationConfigurationAsset : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _placementDuration = 0.3f;
        [SerializeField, Min(0f)] private float _bounceHeight = 60f;
        [SerializeField, Range(0f, 0.3f)] private float _placementSquash = 0.16f;
        [SerializeField, Range(0f, 0.3f)] private float _placementStretch = 0.08f;
        [SerializeField, Range(0f, 0.5f)] private float _placementBounce = 0.09f;
        [SerializeField, Min(0f)] private float _placementSettleDuration = 0.18f;
        [SerializeField, Min(0f)] private float _collapseDuration = 0.3f;
        [SerializeField, Min(0f)] private float _collapseStagger = 0.025f;
        [SerializeField, Min(0f)] private float _collapseBounceHeight = 12f;
        [SerializeField, Range(0f, 0.3f)] private float _collapseSquash = 0.12f;
        [SerializeField, Min(0f)] private float _collapseSettleDuration = 0.22f;
        [SerializeField, Min(0f)] private float _returnDuration = 0.2f;
        [SerializeField, Min(1f)] private float _missPeakScale = 1.15f;
        [SerializeField, Min(0f)] private float _missExpandDuration = 0.1f;
        [SerializeField, Min(0f)] private float _missShrinkDuration = 0.2f;
        [SerializeField, Min(0f)] private float _dropDuration = 0.4f;
        [SerializeField, Min(1f)] private float _dropDistance = 220f;

        public float PlacementDuration => _placementDuration;
        public float BounceHeight => _bounceHeight;
        public float PlacementSquash => _placementSquash;
        public float PlacementStretch => _placementStretch;
        public float PlacementBounce => _placementBounce;
        public float PlacementSettleDuration => _placementSettleDuration;
        public float CollapseDuration => _collapseDuration;
        public float CollapseStagger => _collapseStagger;
        public float CollapseBounceHeight => _collapseBounceHeight;
        public float CollapseSquash => _collapseSquash;
        public float CollapseSettleDuration => _collapseSettleDuration;
        public float ReturnDuration => _returnDuration;
        public float MissPeakScale => _missPeakScale;
        public float MissExpandDuration => _missExpandDuration;
        public float MissShrinkDuration => _missShrinkDuration;
        public float DropDuration => _dropDuration;
        public float DropDistance => _dropDistance;

        public void Validate()
        {
            ConfigurationRange.Require(_placementSquash, 0f, 0.3f, nameof(_placementSquash));
            ConfigurationRange.Require(_placementStretch, 0f, 0.3f, nameof(_placementStretch));
            ConfigurationRange.Require(_placementBounce, 0f, 0.5f, nameof(_placementBounce));
            ConfigurationRange.Require(_placementSettleDuration, 0f, float.MaxValue, nameof(_placementSettleDuration));
            ConfigurationRange.Require(_placementDuration, 0f, float.MaxValue, nameof(_placementDuration));
            ConfigurationRange.Require(_bounceHeight, 0f, float.MaxValue, nameof(_bounceHeight));
            ConfigurationRange.Require(_collapseDuration, 0f, float.MaxValue, nameof(_collapseDuration));
            ConfigurationRange.Require(_collapseStagger, 0f, float.MaxValue, nameof(_collapseStagger));
            ConfigurationRange.Require(_collapseBounceHeight, 0f, float.MaxValue, nameof(_collapseBounceHeight));
            ConfigurationRange.Require(_collapseSquash, 0f, 0.3f, nameof(_collapseSquash));
            ConfigurationRange.Require(_collapseSettleDuration, 0f, float.MaxValue, nameof(_collapseSettleDuration));
            ConfigurationRange.Require(_returnDuration, 0f, float.MaxValue, nameof(_returnDuration));
            ConfigurationRange.Require(_missPeakScale, 1f, float.MaxValue, nameof(_missPeakScale));
            ConfigurationRange.Require(_missExpandDuration, 0f, float.MaxValue, nameof(_missExpandDuration));
            ConfigurationRange.Require(_missShrinkDuration, 0f, float.MaxValue, nameof(_missShrinkDuration));
            ConfigurationRange.Require(_dropDuration, 0f, float.MaxValue, nameof(_dropDuration));
            ConfigurationRange.Require(_dropDistance, 1f, float.MaxValue, nameof(_dropDistance));
        }
    }
}
