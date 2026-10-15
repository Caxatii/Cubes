using System;
using System.Collections.Generic;
using Cubes.Domain.Tower;
using NUnit.Framework;

namespace Cubes.Tests.EditMode.Domain
{
    public sealed class TowerModelTests
    {
        [Test]
        public void FirstCubeAboveScreenIsKeptAndFollowingCubeIsRejected()
        {
            TowerModel tower = CreateTower();
            tower.PlaceFirst(0, 2f);
            Cube first = tower.Cubes[tower.Cubes.Count - 1];
            for (int index = 1; index < 9; index++)
            {
                Assert.That(tower.AddNext(0, new Random(7)), Is.EqualTo(TowerAddResult.Added));
            }

            Assert.That(tower.AddNext(1, new Random(7)), Is.EqualTo(TowerAddResult.LimitReached));
            Assert.That(tower.Cubes.Count, Is.EqualTo(9));
            Assert.That(tower.Cubes[0], Is.SameAs(first));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RemovingAnyCubePreservesSurvivorOrderAndAbsoluteX(int removedIndex)
        {
            TowerModel tower = CreateTower();
            tower.PlaceFirst(0, 2f);
            Cube first = tower.Cubes[tower.Cubes.Count - 1];
            AppendForSetup(tower, new Cube(0, 0.4f));
            AppendForSetup(tower, new Cube(1, 0.8f));
            Cube[] cubes = { tower.Cubes[0], tower.Cubes[1], tower.Cubes[2] };
            float[] positions = { 2f, 2.4f, 2.8f };
            Assert.That(tower.Remove(removedIndex), Is.True);
            int survivor = 0;
            for (int index = 0; index < cubes.Length; index++)
            {
                if (index == removedIndex)
                {
                    continue;
                }

                Assert.That(tower.Cubes[survivor].TypeIndex, Is.EqualTo(cubes[index].TypeIndex));
                Assert.That(tower.Playfield.GetX(tower.BaseX) + tower.Cubes[survivor].HorizontalOffset,
                    Is.EqualTo(positions[index]).Within(0.000001f));
                survivor++;
            }

            Assert.That(tower.Cubes[0].HorizontalOffset, Is.Zero);
        }

        [Test]
        public void BottomRemovalDoesNotClampSurvivorsAroundNewBase()
        {
            TowerModel tower = new TowerModel(new TowerSettings(0.5f, 0.4f), new PlayfieldSettings(7f, 8f));
            tower.PlaceFirst(0, 3f);
            Cube first = tower.Cubes[tower.Cubes.Count - 1];
            AppendForSetup(tower, new Cube(0, 0.4f));
            AppendForSetup(tower, new Cube(0, 0f));
            AppendForSetup(tower, new Cube(0, -0.4f));
            tower.Remove(0);
            Assert.That(tower.Cubes[2].HorizontalOffset, Is.EqualTo(-0.8f).Within(0.000001f));
            TowerModel restored = new TowerModel(tower.Settings, tower.Playfield);
            restored.Restore(tower.Cubes, tower.BaseX);
            Assert.That(restored.Cubes[2].HorizontalOffset, Is.EqualTo(-0.8f).Within(0.000001f));
        }

        [Test]
        public void RemovingLastCubeFreesCapacityAndAllowsNewAnchor()
        {
            TowerModel tower = new TowerModel(new TowerSettings(0.5f, 0.4f), new PlayfieldSettings(7f, 8f));
            tower.PlaceFirst(0, 2f);
            Cube first = tower.Cubes[tower.Cubes.Count - 1];
            tower.ResizePlayfield(7f, 1f);
            tower.AddNext(0, new Random(7));
            Assert.That(tower.IsFull, Is.True);
            tower.Remove(1);
            tower.Remove(0);
            Assert.That(tower.IsFull, Is.False);
            tower.PlaceFirst(0, 5f);
            Cube next = tower.Cubes[tower.Cubes.Count - 1];
            Assert.That(tower.Playfield.GetX(tower.BaseX), Is.EqualTo(5f));
        }

        [Test]
        public void OutOfRangeIndexDoesNotRemoveACube()
        {
            TowerModel tower = CreateTower();
            tower.PlaceFirst(0, 2f);
            Cube first = tower.Cubes[tower.Cubes.Count - 1];
            Assert.That(tower.Remove(99), Is.False);
            Assert.That(tower.Cubes[0], Is.SameAs(first));
        }

        [Test]
        public void RestoreCopiesTheListAndAcceptsNeighborGapCreatedByRemoval()
        {
            TowerModel tower = CreateTower();
            tower.PlaceFirst(0, 2f);
            Cube first = tower.Cubes[tower.Cubes.Count - 1];
            AppendForSetup(tower, new Cube(0, 0.5f));
            AppendForSetup(tower, new Cube(0, 1f));
            tower.Remove(1);
            TowerModel restored = CreateTower();
            restored.Restore(tower.Cubes, tower.BaseX);
            Assert.That(restored.Cubes[0], Is.SameAs(first));
            Assert.That(restored.Cubes[1].HorizontalOffset, Is.EqualTo(1f));
            tower.Clear();
            Assert.That(restored.Cubes.Count, Is.EqualTo(2));
        }

        [Test]
        public void InvalidRestoreLeavesPreviousTowerIntact()
        {
            TowerModel tower = CreateTower();
            tower.PlaceFirst(0, 2f);
            Cube first = tower.Cubes[tower.Cubes.Count - 1];
            IEnumerable<Cube>[] invalid =
            {
                new Cube[] { null },
                new[] { new Cube(0, 0f), new Cube(1, 100f) },
                new[] { new Cube(0, 0.1f) }
            };
            foreach (IEnumerable<Cube> cubes in invalid)
            {
                Assert.Throws<ArgumentException>(() => tower.Restore(cubes, 0.5f));
                Assert.That(tower.Cubes[0], Is.SameAs(first));
                Assert.That(tower.Playfield.GetX(tower.BaseX), Is.EqualTo(2f));
            }
        }

        [Test]
        public void RestoreAboveScreenIsAcceptedAndBlocksFurtherBuilding()
        {
            TowerModel tower = new TowerModel(new TowerSettings(0.5f, 0.4f), new PlayfieldSettings(7f, 1f));
            tower.Restore(new[] { new Cube(0, 0f), new Cube(0, 0f) }, 0.5f);
            Assert.That(tower.IsFull, Is.True);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-0.1f)]
        [TestCase(1.1f)]
        public void InvalidAnchorIsRejected(float value)
        {
            TowerModel tower = CreateTower();
            Assert.Throws<ArgumentOutOfRangeException>(() => tower.Restore(Array.Empty<Cube>(), value));
        }

        [TestCase(5, 0.6f, 0.4f)]
        [TestCase(5, float.NaN, 0.4f)]
        [TestCase(5, 0.5f, -1f)]
        public void InvalidTowerSettingsAreRejected(int limit, float step, float baseOffset)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TowerSettings(step, baseOffset));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidOffsetsAreRejected(float offset)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Cube(0, offset));
        }

        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void NegativeTypeIndicesAreRejected(int typeIndex)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Cube(typeIndex, 0f));
        }

        [Test]
        public void PublicPlacementRequiresTheCorrectTowerState()
        {
            TowerModel tower = CreateTower();
            Assert.Throws<InvalidOperationException>(() => tower.AddNext(0, new Random(7)));
            tower.PlaceFirst(0, 2f);
            Assert.Throws<InvalidOperationException>(() => tower.PlaceFirst(0, 3f));
            Assert.That(tower.Cubes.Count, Is.EqualTo(1));
        }

        private static void AppendForSetup(TowerModel tower, Cube cube)
        {
            List<Cube> cubes = new List<Cube>(tower.Cubes) { cube };
            tower.Restore(cubes, tower.BaseX);
        }

        private static TowerModel CreateTower() => new TowerModel(new TowerSettings(0.5f, 2f), new PlayfieldSettings(7f, 8f));
    }
}
