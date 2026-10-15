using Cubes.Domain.Tower;
using System;
using Cubes.Application.Gameplay;
using Cubes.Presentation.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Cubes.Tests.EditMode.Presentation
{
    public sealed class BuildAreaTests
    {
        [TestCase(0f, 0f)]
        [TestCase(190f, 140f)]
        [TestCase(-190f, -140f)]
        public void PointerConversionKeepsBothCoordinatesWithoutApplyingPlacementRules(float x, float y)
        {
            GameObject field = new GameObject("Field", typeof(RectTransform), typeof(BuildAreaView));
            try
            {
                RectTransform rect = (RectTransform)field.transform;
                rect.sizeDelta = new Vector2(400f, 300f);
                TowerState state = new TowerState(4f, 3f, 0f, Array.Empty<Cube>());
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(new Vector2(x, y)));
                Assert.That(field.GetComponent<BuildAreaView>().TryGetLogicalPosition(state, new Vector2(100f, 100f),
                    screen, null, out Vector2 position), Is.True);
                Assert.That(position.x, Is.EqualTo((x + 200f) / 100f).Within(0.00001f));
                Assert.That(position.y, Is.EqualTo((y + 150f) / 100f).Within(0.00001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(field);
            }
        }

        [TestCase(1920f, 1080f)]
        [TestCase(1440f, 1080f)]
        [TestCase(2520f, 1080f)]
        [TestCase(1080f, 1920f)]
        public void CornersAndEdgesAreAvailableWithScaledNonCenteredRect(float width, float height)
        {
            GameObject field = new GameObject("Field", typeof(RectTransform), typeof(BuildAreaView));
            try
            {
                RectTransform rect = (RectTransform)field.transform;
                rect.pivot = new Vector2(0.2f, 0.7f);
                rect.sizeDelta = new Vector2(width, height);
                rect.localScale = Vector3.one * 0.7f;
                rect.position = new Vector3(300f, 400f, 0f);
                TowerState state = new TowerState(7f, 8f, 0f, Array.Empty<Cube>());
                foreach (float u in new[] { 0.001f, 0.5f, 0.999f })
                foreach (float v in new[] { 0.001f, 0.5f, 0.999f })
                {
                    Vector2 local = rect.rect.min + Vector2.Scale(rect.rect.size, new Vector2(u, v));
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(local));
                    Assert.That(field.GetComponent<BuildAreaView>().TryGetLogicalPosition(state, new Vector2(180f, 180f), screen, null, out _), Is.True);
                }

                Vector2 outside = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.max + Vector2.one));
                Assert.That(field.GetComponent<BuildAreaView>().TryGetLogicalPosition(state, new Vector2(180f, 180f), outside, null, out _), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(field);
            }
        }

        [Test]
        public void FormerSideMarginsAcceptPlacement()
        {
            GameObject field = new GameObject("Field", typeof(RectTransform), typeof(BuildAreaView));
            try
            {
                ((RectTransform)field.transform).sizeDelta = new Vector2(800f, 300f);
                TowerState state = new TowerState(4f, 3f, 0f, Array.Empty<Cube>());
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, field.transform.TransformPoint(new Vector2(350f, 0f)));
                Assert.That(field.GetComponent<BuildAreaView>().TryGetLogicalPosition(state, new Vector2(100f, 100f), screen, null, out Vector2 logical), Is.True);
                Assert.That(logical.x, Is.EqualTo(7.5f).Within(0.00001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(field);
            }
        }
    }
}
