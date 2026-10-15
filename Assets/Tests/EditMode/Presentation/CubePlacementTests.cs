using Cubes.Presentation.Configuration;
using Cubes.Presentation.Gameplay;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;

namespace Cubes.Tests.EditMode.Presentation
{
    public sealed class CubePlacementTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void LandingKeepsBottomAtContactAndCompletesAtExactTarget(bool first)
        {
            GameObject cube = new GameObject("Cube", typeof(RectTransform), typeof(CubeView));
            AnimationConfigurationAsset settings = ScriptableObject.CreateInstance<AnimationConfigurationAsset>();
            Tween tween = null;
            try
            {
                CubeView view = cube.GetComponent<CubeView>();
                view.RectTransform.sizeDelta = new Vector2(180f, 180f);
                view.RectTransform.anchoredPosition = new Vector2(-150f, 350f);
                Vector2 target = new Vector2(100f, 90f);
                Vector3 scale = Vector3.one * 0.7f;
                tween = (first ? view.SettleTo(target, settings, scale) : view.PlaceTo(target, scale, settings)).Pause();
                float flight = first ? settings.CollapseDuration : settings.PlacementDuration;
                tween.Goto(flight + (first ? settings.CollapseSettleDuration : settings.PlacementSettleDuration) * 0.25f);
                float bottom = view.RectTransform.anchoredPosition.y - view.Size.y * view.RectTransform.localScale.y * 0.5f;
                Assert.That(bottom, Is.EqualTo(target.y - view.Size.y * scale.y * 0.5f).Within(0.001f));
                Assert.That(view.RectTransform.localScale.y, Is.LessThan(scale.y));
                tween.Complete(true);
                Assert.That(Vector2.Distance(view.RectTransform.anchoredPosition, target), Is.LessThan(0.0001f));
                Assert.That(Vector3.Distance(view.RectTransform.localScale, scale), Is.LessThan(0.0001f));
            }
            finally
            {
                tween?.Kill();
                Object.DestroyImmediate(cube);
                Object.DestroyImmediate(settings);
            }
        }
    }
}
