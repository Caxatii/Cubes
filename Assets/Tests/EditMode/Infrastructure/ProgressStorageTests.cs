using System;
using System.Linq;
using System.IO;
using Cubes.Application.Configuration;
using Cubes.Application.Gameplay;
using Cubes.Application.Progress;
using Cubes.Domain.Tower;
using Cubes.Infrastructure.Progress;
using NUnit.Framework;

namespace Cubes.Tests.EditMode.Infrastructure
{
    public sealed class ProgressStorageTests
    {
        private string _directory;
        private string _path;
        private OdinProgressStorage _storage;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(UnityEngine.Application.dataPath, "../LocalDocs/GameplayValidation/Storage-" + Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_directory, "progress.odin");
            _storage = new OdinProgressStorage(_path);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Test]
        public void MissingSaveDoesNotCreateAFile()
        {
            Assert.That(_storage.Load(), Is.Null);
            Assert.That(Directory.Exists(_directory), Is.False);
        }

        [Test]
        public void SaveAndLoadPreserveLanguageDimensionsOrderAndRepeatedCubeTypes()
        {
            TowerState state = new TowerState(14f, 8f, 0.3f,
                new[] { new Cube(0, 0f), new Cube(0, 0.4f), new Cube(19, 0.8f) }, 7f, 8f);
            _storage.Save(new GameProgress("en", state));

            GameProgress loaded = new OdinProgressStorage(_path).Load();
            Assert.That(loaded.Language, Is.EqualTo("en"));
            Assert.That(loaded.Tower.PlayfieldWidth, Is.EqualTo(14f));
            Assert.That(loaded.Tower.PlayfieldHeight, Is.EqualTo(8f));
            Assert.That(loaded.Tower.ReferenceWidth, Is.EqualTo(7f));
            Assert.That(loaded.Tower.ReferenceHeight, Is.EqualTo(8f));
            Assert.That(loaded.Tower.BaseX, Is.EqualTo(0.3f));
            Assert.That(loaded.Tower.Cubes.Count, Is.EqualTo(3));
            for (int index = 0; index < state.Cubes.Count; index++)
            {
                Assert.That(loaded.Tower.Cubes[index].TypeIndex, Is.EqualTo(state.Cubes[index].TypeIndex));
                Assert.That(loaded.Tower.Cubes[index].HorizontalOffset, Is.EqualTo(state.Cubes[index].HorizontalOffset));
            }

            GameSession session = new GameSession(new GameConfiguration(Enumerable.Repeat(CubeColor.Red, 20).ToArray(), 0.5f, 0.4f, 7f, 8f));
            Assert.DoesNotThrow(() => session.Restore(loaded.Tower));
        }

        [Test]
        public void RepeatedSavesReplaceTheFileAndResetCanBeRestored()
        {
            _storage.Save(new GameProgress("ru", new TowerState(7f, 8f, 0.5f, new[] { new Cube(3, 0f) })));
            _storage.Save(new GameProgress("en", new TowerState(7f, 8f, 0f, Array.Empty<Cube>())));
            GameProgress loaded = _storage.Load();
            Assert.That(loaded.Language, Is.EqualTo("en"));
            Assert.That(loaded.Tower.Cubes, Is.Empty);
            Assert.That(Directory.GetFiles(_directory).Length, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ConsecutiveGameplaySavesLoadInANewSessionWithoutMissingCubes(bool readBetweenSaves)
        {
            GameConfiguration configuration = new GameConfiguration(Enumerable.Repeat(CubeColor.Red, 20).ToArray(), 0.5f, 0.4f, 7f, 8f);
            GameSession session = new GameSession(configuration, new Random(7));
            session.PlaceFirst(0, 2f);
            _storage.Save(new GameProgress("ru", session.CreateSnapshot()));
            for (int index = 0; index < 3; index++)
            {
                if (readBetweenSaves)
                {
                    Assert.That(_storage.Load().Tower.Cubes.Count, Is.EqualTo(index + 1));
                }

                session.AddNext(index);
                _storage.Save(new GameProgress("en", session.CreateSnapshot()));
            }

            TowerState expected = session.CreateSnapshot();
            GameProgress loaded = new OdinProgressStorage(_path).Load();
            Assert.That(loaded.Tower.Cubes.Count, Is.EqualTo(expected.Cubes.Count));
            for (int index = 0; index < expected.Cubes.Count; index++)
            {
                Assert.That(loaded.Tower.Cubes[index], Is.Not.Null, "Cube " + index);
                Assert.That(loaded.Tower.Cubes[index].TypeIndex, Is.EqualTo(expected.Cubes[index].TypeIndex));
                Assert.That(loaded.Tower.Cubes[index].HorizontalOffset, Is.EqualTo(expected.Cubes[index].HorizontalOffset));
            }

            Assert.DoesNotThrow(() => new GameSession(configuration).Restore(loaded.Tower));
        }

        [Test]
        public void DamagedFileIsRejectedWithoutChangingIt()
        {
            Directory.CreateDirectory(_directory);
            byte[] damaged = { 1, 2, 3 };
            File.WriteAllBytes(_path, damaged);
            Assert.Throws<InvalidDataException>(() => _storage.Load());
            Assert.That(File.ReadAllBytes(_path), Is.EqualTo(damaged));
        }

        [Test]
        public void FailedWriteDoesNotTruncateThePreviousSave()
        {
            GameProgress progress = new GameProgress("en", new TowerState(7f, 8f, 0f, Array.Empty<Cube>()));
            _storage.Save(progress);
            byte[] original = File.ReadAllBytes(_path);
            using (FileStream locked = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Assert.Throws<IOException>(() => _storage.Save(progress));
            }

            Assert.That(File.ReadAllBytes(_path), Is.EqualTo(original));
        }
    }
}
