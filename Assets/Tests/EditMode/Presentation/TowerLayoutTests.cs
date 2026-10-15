using Cubes.Domain.Tower;
using System;
using Cubes.Application.Gameplay;
using Cubes.Presentation.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Cubes.Tests.EditMode.Presentation
{
    public sealed class TowerLayoutTests
    {
        [TestCase(1440f, 1080f)]
        [TestCase(1920f, 1080f)]
        [TestCase(2340f, 1080f)]
        public void LogicalRoundTripAndCubeAspectAreIndependentOfResolution(float width, float height)
        {
            TowerState state = new TowerState(7f, 8f, 0.3f, new[] { new Cube(0, 0f) });
            Rect bounds = new Rect(-width * 0.2f, -height * 0.3f, width * 0.4f, height * 0.6f);
            Vector2 prefab = new Vector2(180f, 180f);
            Vector2 logical = new Vector2(2f, 6f);
            Vector2 local = new TowerLayout(state, bounds, prefab).ToLocal(logical);
            Assert.That(Vector2.Distance(new TowerLayout(state, bounds, prefab).ToLogical(local), logical), Is.LessThan(0.00001f));
            Vector2 size = new TowerLayout(state, bounds, prefab).CubeSize;
            Assert.That(size.x, Is.EqualTo(size.y).Within(0.00001f));
        }

        [Test]
        public void RepeatedLayoutDoesNotChangeStateOrShrinkATallTower()
        {
            Cube[] cubes = new Cube[100];
            for (int index = 0; index < cubes.Length; index++)
            {
                cubes[index] = new Cube(0, 0f);
            }

            TowerState state = new TowerState(7f, 8f, 0.3f, cubes);
            Rect bounds = new Rect(0f, 0f, 700f, 800f);
            Vector2 prefab = new Vector2(180f, 180f);
            Vector2 first = new TowerLayout(state, bounds, prefab).GetCubePosition(0);
            Vector2 last = new TowerLayout(state, bounds, prefab).GetCubePosition(99);
            for (int iteration = 0; iteration < 20; iteration++)
            {
                Assert.That(new TowerLayout(state, bounds, prefab).GetCubePosition(0), Is.EqualTo(first));
                Assert.That(Vector2.Distance(new TowerLayout(state, bounds, prefab).CubeSize, new Vector2(100f, 100f)), Is.LessThan(0.0001f));
            }

            Assert.That(last.y - first.y, Is.EqualTo(9900f).Within(0.001f));
            Assert.That(state.Cubes[0].TypeIndex, Is.Zero);
            Assert.That(state.BaseX, Is.EqualTo(0.3f));
        }

        [TestCase(400f, 800f)]
        [TestCase(800f, 400f)]
        [TestCase(1920f, 1080f)]
        public void FirstCubeRestsOnBuildAreaFloorAtEveryAspectRatio(float width, float height)
        {
            TowerState state = new TowerState(7f, 8f, 0.5f, new[] { new Cube(0, 0f) });
            Rect bounds = new Rect(-width * 0.5f, -height * 0.5f, width, height);
            Vector2 prefab = new Vector2(180f, 180f);
            Vector2 size = new TowerLayout(state, bounds, prefab).CubeSize;
            Vector2 position = new TowerLayout(state, bounds, prefab).GetCubePosition(0);
            Assert.That(position.y - size.y * 0.5f, Is.EqualTo(bounds.yMin).Within(0.0001f));
        }

        [Test]
        public void ImpossibleDisplayAreaIsDiagnosed()
        {
            TowerState state = new TowerState(7f, 8f, 0f, Array.Empty<Cube>());
            Assert.Throws<InvalidOperationException>(() => new TowerLayout(state, new Rect(0f, 0f, 1f, 1f), new Vector2(180f, 180f)));
        }

        [TestCase(1920f, 1080f)]
        [TestCase(1440f, 1080f)]
        [TestCase(2520f, 1080f)]
        [TestCase(1080f, 1920f)]
        public void EntireBuildAreaMapsToAdaptiveLogicalField(float width, float height)
        {
            TowerState state = new TowerState(7f, 8f, 0.5f, Array.Empty<Cube>());
            Rect bounds = new Rect(-width * 0.3f, -height * 0.7f, width, height);
            Vector2 prefab = new Vector2(180f, 180f);
            Vector2 logicalSize = new TowerLayout(state, bounds, prefab).LogicalSize;
            Assert.That(Vector2.Distance(new TowerLayout(state, bounds, prefab).ToLogical(bounds.min), Vector2.zero), Is.LessThan(0.0001f));
            Assert.That(Vector2.Distance(new TowerLayout(state, bounds, prefab).ToLogical(bounds.max), logicalSize), Is.LessThan(0.0001f));
            TowerState right = new TowerState(logicalSize.x, logicalSize.y, 1f, new[] { new Cube(0, 0f) }, 7f, 8f);
            Vector2 cube = new TowerLayout(right, bounds, prefab).CubeSize;
            Vector2 position = new TowerLayout(right, bounds, prefab).GetCubePosition(0);
            Assert.That(position.x + cube.x * 0.5f, Is.EqualTo(bounds.xMax).Within(0.001f));
            Assert.That(position.y - cube.y * 0.5f, Is.EqualTo(bounds.yMin).Within(0.001f));
        }

        [Test]
        public void ResizingBackDoesNotAccumulateCubeShrinkage()
        {
            TowerState state = new TowerState(7f, 8f, 0.5f, Array.Empty<Cube>());
            Vector2 prefab = new Vector2(180f, 180f);
            Rect original = new Rect(0f, 0f, 700f, 800f);
            for (int i = 0; i < 10; i++)
            {
                Vector2 wide = new TowerLayout(state, new Rect(0f, 0f, 1800f, 500f), prefab).LogicalSize;
                state = new TowerState(wide.x, wide.y, state.BaseX, state.Cubes, 7f, 8f);
                Vector2 narrow = new TowerLayout(state, original, prefab).LogicalSize;
                state = new TowerState(narrow.x, narrow.y, state.BaseX, state.Cubes, 7f, 8f);
                Assert.That(Vector2.Distance(new TowerLayout(state, original, prefab).CubeSize, new Vector2(100f, 100f)), Is.LessThan(0.0001f));
            }
        }
    }
}
