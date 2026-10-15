using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cubes.Application.Configuration;
using Cubes.Application.Gameplay;
using Cubes.Application.Localization;
using Cubes.Application.Progress;
using Cubes.Domain.Tower;
using Cubes.Infrastructure.Progress;
using NUnit.Framework;

namespace Cubes.Tests.EditMode.Application
{
    public sealed class ImprovementPlanTests
    {
        private string _directory;
        private string SavePath => Path.Combine(_directory, "progress.odin");
        private static GameConfiguration Configuration => new GameConfiguration(
            new[] { CubeColor.Red, CubeColor.Blue, CubeColor.Red }, 0.5f, 0.4f, 7f, 8f);

        [SetUp]
        public void SetUp()
        {
            _directory = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,
                "../LocalDocs/GameplayValidation/Recovery-" + Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_directory, true);

        [TestCase(CubeColor.Unknown)]
        [TestCase((CubeColor)999)]
        public void InvalidColorCannotEnterGameplay(CubeColor color)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new GameConfiguration(new[] { color }, 0.5f, 0.4f, 7f, 8f));

        [Test]
        public void ConfigurationCopiesItsColors()
        {
            CubeColor[] colors = { CubeColor.Red };
            GameConfiguration configuration = new GameConfiguration(colors, 0.5f, 0.4f, 7f, 8f);
            colors[0] = CubeColor.Blue;
            Assert.That(configuration.GetColor(0), Is.EqualTo(CubeColor.Red));
        }

        [Test]
        public void MemorySourceCanCreateAWorkingSession()
        {
            IGameConfigurationSource source = new MemorySource();
            GameSession session = new GameSession(source.CreateConfiguration());
            session.PlaceFirst(2, 3f);
            Assert.That(session.AddNext(1, 0), Is.EqualTo(TowerAddResult.Added));
        }

        [Test]
        public void MatchingRuleUsesGeometricTargetAndAllowsDifferentTypesOfSameColor()
        {
            GameSession session = new GameSession(Configuration, new Random(7), new MatchingColorPlacementRule());
            session.Restore(new TowerState(7f, 8f, 0.5f, new[] { new Cube(0, 0f), new Cube(1, 0f) }));
            Assert.That(session.AddNext(2, 1), Is.EqualTo(TowerAddResult.ColorMismatch));
            Assert.That(session.CreateSnapshot().Cubes.Count, Is.EqualTo(2));
            Assert.That(session.AddNext(2, 0), Is.EqualTo(TowerAddResult.Added));
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void StaleTargetDoesNotModifyTheTower(int targetIndex)
        {
            GameSession session = new GameSession(Configuration);
            session.PlaceFirst(0, 3f);
            Assert.That(session.AddNext(2, targetIndex), Is.EqualTo(TowerAddResult.InvalidTarget));
            Assert.That(session.CreateSnapshot().Cubes.Count, Is.EqualTo(1));
        }

        [TestCase(1f, 2)]
        [TestCase(5f, 6)]
        [TestCase(6.4f, 7)]
        [TestCase(12f, 13)]
        public void FirstCubeCrossingScreenIsAcceptedThenBuildingStops(float height, int expectedCount)
        {
            GameSession session = new GameSession(Configuration);
            session.ResizePlayfield(7f, height);
            session.PlaceFirst(0, 3f);
            while (!session.IsHeightLimitReached)
                Assert.That(session.AddNext(0, 0), Is.EqualTo(TowerAddResult.Added));
            Assert.That(session.CreateSnapshot().Cubes.Count, Is.EqualTo(expectedCount));
            Assert.That(session.AddNext(1, 0), Is.EqualTo(TowerAddResult.LimitReached));
            session.Remove(0);
            Assert.That(session.IsHeightLimitReached, Is.False);
            Assert.That(session.AddNext(1, 0), Is.EqualTo(TowerAddResult.Added));
        }

        [Test]
        public void ResizeAndRestoreRecomputeLimitWithoutChangingCubes()
        {
            GameSession session = new GameSession(Configuration);
            session.Restore(new TowerState(7f, 8f, 0.5f, Enumerable.Repeat(new Cube(0, 0f), 9)));
            Assert.That(session.IsHeightLimitReached, Is.True);
            session.ResizePlayfield(7f, 12f);
            Assert.That(session.IsHeightLimitReached, Is.False);
            session.ResizePlayfield(7f, 3f);
            Assert.That(session.IsHeightLimitReached, Is.True);
            Assert.That(session.CreateSnapshot().Cubes.Count, Is.EqualTo(9));
        }

        [Test]
        public void LegacyBinaryKeepsTypeOrderOffsetsAndLanguage()
        {
            string legacy = Path.Combine(UnityEngine.Application.dataPath, "Tests/EditMode/Infrastructure/legacy-progress.bytes");
            File.Copy(legacy, SavePath);
            OdinProgressStorage storage = new OdinProgressStorage(SavePath);
            GameProgress loaded = storage.Load();
            Assert.That(loaded.FormatVersion, Is.EqualTo(0));
            Assert.That(loaded.Language, Is.EqualTo("en"));
            Assert.That(loaded.Tower.Cubes.Select(c => c.TypeIndex), Is.EqualTo(new[] { 0, 19 }));
            Assert.That(loaded.Tower.Cubes[1].HorizontalOffset, Is.EqualTo(0.4f));
            storage.Save(new GameProgress(loaded.Language, loaded.Tower));
            Assert.That(storage.Load().FormatVersion, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CorruptOrMissingPrimaryRecoversBackupWithoutWriting(bool missing)
        {
            OdinProgressStorage storage = PrepareTwoSaves();
            byte[] backup = File.ReadAllBytes(SavePath + ".bak");
            if (missing) File.Delete(SavePath);
            else File.WriteAllBytes(SavePath, new byte[] { 1, 2, 3 });
            var localization = new Localization();
            GameSession session = new GameSession(Configuration);
            Assert.That(new ProgressService(storage).TryLoad(session, localization, out string diagnostic), Is.True);
            Assert.That(diagnostic, Is.Not.Empty);
            Assert.That(localization.Language, Is.EqualTo("ru"));
            Assert.That(session.CreateSnapshot().Cubes[0].TypeIndex, Is.EqualTo(0));
            Assert.That(File.ReadAllBytes(SavePath + ".bak"), Is.EqualTo(backup));
            Assert.That(File.Exists(SavePath), Is.EqualTo(!missing));
            storage.Save(new GameProgress("en", session.CreateSnapshot()));
            Assert.That(File.ReadAllBytes(SavePath + ".bak"), Is.EqualTo(backup));
        }

        [Test]
        public void CatalogInvalidPrimaryAlsoRecoversBackup()
        {
            OdinProgressStorage storage = PrepareTwoSaves();
            storage.Save(Progress(19, "en"));
            GameSession session = new GameSession(Configuration);
            Assert.That(new ProgressService(storage).TryLoad(session, new Localization(), out _), Is.True);
            Assert.That(session.CreateSnapshot().Cubes[0].TypeIndex, Is.EqualTo(1));
        }

        [Test]
        public void TwoCorruptCopiesLeaveCleanSessionAndFilesIntact()
        {
            OdinProgressStorage storage = PrepareTwoSaves();
            File.WriteAllText(SavePath, "broken");
            File.WriteAllText(SavePath + ".bak", "also broken");
            GameSession session = new GameSession(Configuration);
            Assert.That(new ProgressService(storage).TryLoad(session, new Localization(), out string diagnostic), Is.False);
            Assert.That(diagnostic, Is.Not.Empty);
            Assert.That(session.CreateSnapshot().Cubes, Is.Empty);
            Assert.That(File.ReadAllText(SavePath), Is.EqualTo("broken"));
            Assert.That(File.ReadAllText(SavePath + ".bak"), Is.EqualTo("also broken"));
        }

        [Test]
        public void OrphanTemporaryFileIsIgnoredAndNextSaveReplacesIt()
        {
            OdinProgressStorage storage = PrepareTwoSaves();
            File.WriteAllText(SavePath + ".tmp", "incomplete write");
            Assert.That(storage.Load().Language, Is.EqualTo("en"));
            storage.Save(Progress(2, "ru"));
            Assert.That(storage.Load().Tower.Cubes[0].TypeIndex, Is.EqualTo(2));
            Assert.That(File.Exists(SavePath + ".tmp"), Is.False);
        }

        private OdinProgressStorage PrepareTwoSaves()
        {
            OdinProgressStorage storage = new OdinProgressStorage(SavePath);
            storage.Save(Progress(0, "ru"));
            storage.Save(Progress(1, "en"));
            return storage;
        }

        private static GameProgress Progress(int type, string language)
            => new GameProgress(language, new TowerState(7f, 8f, 0.5f, new[] { new Cube(type, 0f) }));

        private sealed class MemorySource : IGameConfigurationSource
        {
            public GameConfiguration CreateConfiguration() => Configuration;
        }

        private sealed class Localization : IGameLocalization
        {
            public string Language { get; private set; } = "en";
            public IReadOnlyList<string> AvailableLanguages { get; } = new[] { "ru", "en" };
            public event Action Changed;
            public string Get(string key) => key;
            public void SetLanguage(string language) { Language = language; Changed?.Invoke(); }
        }
    }
}
