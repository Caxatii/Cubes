using System;
using Cubes.Domain.Tower;
using NUnit.Framework;

namespace Cubes.Tests.EditMode.Domain
{
    public sealed class TowerPlacementTests
    {
        [TestCase(0f, 0f, 0.5f, 0.5f)]
        [TestCase(7f, 8f, 6.5f, 0.5f)]
        [TestCase(0f, 8f, 0.5f, 0.5f)]
        [TestCase(7f, 0f, 6.5f, 0.5f)]
        [TestCase(2f, 6f, 2f, 0.5f)]
        public void FirstPlacementLandsOnFloorAndClampsHorizontalPosition(float x, float y, float expectedX, float expectedY)
        {
            TowerModel tower = CreateTower();
            tower.PlaceFirst(0, x);
            Cube cube = tower.Cubes[tower.Cubes.Count - 1];
            Assert.That(tower.Playfield.GetX(tower.BaseX), Is.EqualTo(expectedX).Within(0.000001f));
        }

        [Test]
        public void RandomPlacementsStayWithinIntersectionUntilScreenLimit()
        {
            TowerModel tower = CreateTower();
            tower.PlaceFirst(0, 0.6f);
            Cube first = tower.Cubes[tower.Cubes.Count - 1];
            Random random = new Random(17);
            float previous = 0f;
            for (int index = 0; index < 8; index++)
            {
                Assert.That(tower.AddNext(0, random), Is.EqualTo(TowerAddResult.Added));
                Cube cube = tower.Cubes[tower.Cubes.Count - 1];
                Assert.That(cube.HorizontalOffset, Is.InRange(-0.100001f, 0.4f));
                Assert.That(Math.Abs(cube.HorizontalOffset - previous), Is.LessThanOrEqualTo(0.500001f));
                previous = cube.HorizontalOffset;
            }

            Assert.That(tower.Cubes.Count, Is.EqualTo(9));
        }

        [Test]
        public void IdenticalSeedsProduceIdenticalOffsets()
        {
            TowerModel left = CreateTower();
            TowerModel right = CreateTower();
            left.PlaceFirst(0, 2f);
            Cube firstLeft = left.Cubes[left.Cubes.Count - 1];
            right.PlaceFirst(0, 2f);
            Cube firstRight = right.Cubes[right.Cubes.Count - 1];
            Random leftRandom = new Random(31);
            Random rightRandom = new Random(31);
            for (int index = 0; index < 8; index++)
            {
                left.AddNext(0, leftRandom);
                Cube leftCube = left.Cubes[left.Cubes.Count - 1];
                right.AddNext(0, rightRandom);
                Cube rightCube = right.Cubes[right.Cubes.Count - 1];
                Assert.That(leftCube.HorizontalOffset, Is.EqualTo(rightCube.HorizontalOffset));
            }
        }

        [Test]
        public void ARangeWithOnePointIsAccepted()
        {
            TowerModel tower = new TowerModel(new TowerSettings(0f, 0f), new PlayfieldSettings(1f, 1f));
            tower.PlaceFirst(0, 0.8f);
            Cube first = tower.Cubes[tower.Cubes.Count - 1];
            Assert.That(tower.AddNext(0, new Random(7)), Is.EqualTo(TowerAddResult.Added));
            Cube cube = tower.Cubes[tower.Cubes.Count - 1];
            Assert.That(cube.HorizontalOffset, Is.Zero);
        }

        [Test]
        public void EmptyIntersectionAfterRemovalRejectsPlacementWithoutMovingSurvivors()
        {
            TowerModel tower = CreateTower();
            tower.Restore(new[] { new Cube(0, 0f), new Cube(0, 1.2f) },
                0.5f);
            Assert.That(tower.AddNext(0, new Random(7)), Is.EqualTo(TowerAddResult.InvalidOffset));
            Assert.That(tower.Cubes.Count, Is.EqualTo(2));
            Assert.That(tower.Cubes[1].HorizontalOffset, Is.EqualTo(1.2f));
        }

        [Test]
        public void FullTowerDoesNotConsumeRandomValuesOrCreateCubes()
        {
            TowerModel tower = new TowerModel(new TowerSettings(0.5f, 0.4f), new PlayfieldSettings(7f, 8f));
            tower.PlaceFirst(0, 2f);
            Cube first = tower.Cubes[tower.Cubes.Count - 1];
            tower.ResizePlayfield(7f, 1f);
            tower.AddNext(0, new Random(7));
            Random random = new Random(7);
            Assert.That(tower.AddNext(0, random), Is.EqualTo(TowerAddResult.LimitReached));
            Assert.That(tower.Cubes.Count, Is.EqualTo(2));
            Assert.That(random.Next(), Is.EqualTo(new Random(7).Next()));
        }

        [TestCase(0f)]
        [TestCase(0.9f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidLogicalDimensionsAreRejected(float size)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayfieldSettings(size, 8f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayfieldSettings(7f, size));
        }

        private static TowerModel CreateTower() => new TowerModel(new TowerSettings(0.5f, 0.4f), new PlayfieldSettings(7f, 8f));
    }
}
