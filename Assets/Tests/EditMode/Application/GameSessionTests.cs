using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Cubes.Application.Configuration;
using Cubes.Application.Gameplay;
using Cubes.Application.Localization;
using Cubes.Application.Progress;
using Cubes.Domain.Tower;
using NUnit.Framework;

namespace Cubes.Tests.EditMode.Application
{
    public sealed class GameSessionTests
    {
        private TestStorage _storage;
        private TestLocalization _localization;
        private GameSession _session;
        private ProgressService _progress;

        [SetUp]
        public void SetUp()
        {
            _storage = new TestStorage();
            _localization = new TestLocalization();
            _session = CreateSession();
            _progress = new ProgressService(_storage);
        }

        [Test]
        public void MissingProgressHasNoDiagnosticAndDoesNotWrite()
        {
            Assert.That(_progress.TryLoad(_session, _localization, out string error), Is.False);
            Assert.That(error, Is.Null);
            Assert.That(_storage.Writes, Is.Zero);
        }

        [Test]
        public void ActionsChangeTheModelWithoutWritingFilesOrMutatingOldSnapshots()
        {
            TowerState empty = _session.CreateSnapshot();
            _session.PlaceFirst(0, 2f);
            TowerState first = _session.CreateSnapshot();
            Assert.That(_session.AddNext(1), Is.EqualTo(TowerAddResult.Added));
            Assert.That(_session.Remove(0), Is.True);
            Assert.That(empty.Cubes, Is.Empty);
            Assert.That(first.Cubes.Count, Is.EqualTo(1));
            Assert.That(first.Cubes[0].HorizontalOffset, Is.Zero);
            Assert.That(first.BaseX, Is.EqualTo(0.25f));
            Assert.That(_session.CreateSnapshot().Cubes[0].TypeIndex, Is.EqualTo(1));
            Assert.That(_storage.Writes, Is.Zero);
        }

        [TestCase(-1)]
        [TestCase(2)]
        public void InvalidTypesAreRejectedWithoutChangingTheTower(int type)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _session.PlaceFirst(type, 2f));
            Assert.That(_session.CreateSnapshot().Cubes, Is.Empty);
            _session.PlaceFirst(0, 2f);
            Assert.Throws<ArgumentOutOfRangeException>(() => _session.AddNext(type));
            Assert.That(_session.CreateSnapshot().Cubes.Count, Is.EqualTo(1));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-1f)]
        [TestCase(8f)]
        public void InvalidFirstPlacementDoesNotChangeState(float x)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _session.PlaceFirst(0, x));
            Assert.That(_session.CreateSnapshot().Cubes, Is.Empty);
        }

        [Test]
        public void LimitAndInvalidRemovalLeaveTheTowerIntact()
        {
            _session.PlaceFirst(0, 2f);
            for (int index = 1; index < 9; index++)
            {
                Assert.That(_session.AddNext(0), Is.EqualTo(TowerAddResult.Added));
            }

            Assert.That(_session.AddNext(1), Is.EqualTo(TowerAddResult.LimitReached));
            Assert.That(_session.Remove(-1), Is.False);
            Assert.That(_session.Remove(9), Is.False);
            Assert.That(_session.CreateSnapshot().Cubes.Count, Is.EqualTo(9));
            Assert.That(_storage.Writes, Is.Zero);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RemovalPreservesAbsolutePositionsAndSurvivesSaveAndReload(int removedIndex)
        {
            _session.PlaceFirst(0, 2f);
            _session.AddNext(1);
            _session.AddNext(0);
            TowerState before = _session.CreateSnapshot();
            _session.Remove(removedIndex);
            TowerState after = _session.CreateSnapshot();
            int survivor = 0;
            for (int index = 0; index < before.Cubes.Count; index++)
            {
                if (index == removedIndex) continue;
                Assert.That(after.Cubes[survivor].TypeIndex, Is.EqualTo(before.Cubes[index].TypeIndex));
                Assert.That(0.5f + after.BaseX * 6f + after.Cubes[survivor].HorizontalOffset,
                    Is.EqualTo(0.5f + before.BaseX * 6f + before.Cubes[index].HorizontalOffset).Within(0.000001f));
                survivor++;
            }

            Assert.That(_session.AddNext(1), Is.EqualTo(TowerAddResult.Added));
            TowerState saved = _session.CreateSnapshot();
            Assert.That(_progress.TrySave(saved, "en", out string error), Is.True);
            Assert.That(error, Is.Null);
            GameSession loaded = CreateSession();
            Assert.That(_progress.TryLoad(loaded, _localization, out error), Is.True);
            Assert.That(error, Is.Null);
            Assert.That(_localization.Language, Is.EqualTo("en"));
            TowerState restored = loaded.CreateSnapshot();
            for (int index = 0; index < saved.Cubes.Count; index++)
            {
                Assert.That(restored.Cubes[index].TypeIndex, Is.EqualTo(saved.Cubes[index].TypeIndex));
                Assert.That(restored.Cubes[index].HorizontalOffset, Is.EqualTo(saved.Cubes[index].HorizontalOffset));
            }
        }

        [Test]
        public void SaveFailureKeepsTheCompletedActionAndReportsTheReason()
        {
            _session.PlaceFirst(0, 2f);
            _storage.FailWrite = true;
            Assert.That(_progress.TrySave(_session.CreateSnapshot(), "en", out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(_session.CreateSnapshot().Cubes.Count, Is.EqualTo(1));
        }

        [Test]
        public void InvalidProgressDoesNotPartiallyRestoreOrChangeLanguage()
        {
            _session.PlaceFirst(0, 2f);
            TowerState before = _session.CreateSnapshot();
            TowerState[] invalid =
            {
                State(new Cube(0, 0f), new Cube(2, 0f)),
                State(new Cube[] { null }),
                State(new Cube(0, 0f), new Cube(1, 100f)),
                new TowerState(9f, 8f, 0f, Array.Empty<Cube>())
            };
            foreach (TowerState state in invalid)
            {
                _storage.Value = new GameProgress("en", state);
                Assert.That(_progress.TryLoad(_session, _localization, out string error), Is.False);
                Assert.That(error, Is.Not.Null.And.Not.Empty);
                Assert.That(_session.CreateSnapshot().Cubes[0], Is.SameAs(before.Cubes[0]));
                Assert.That(_localization.Language, Is.EqualTo("ru"));
                Assert.That(_storage.Writes, Is.Zero);
            }
        }

        [Test]
        public void ReadFailureAndProgrammingErrorsAreHandledSeparately()
        {
            _storage.FailRead = true;
            Assert.That(_progress.TryLoad(_session, _localization, out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            _storage.ProgrammingError = true;
            Assert.Throws<InvalidOperationException>(() => _progress.TryLoad(_session, _localization, out _));
            Assert.Throws<InvalidOperationException>(() => _progress.TrySave(_session.CreateSnapshot(), "ru", out _));
        }

        [Test]
        public void ResetAndLanguageSelectionAreIndependent()
        {
            _session.PlaceFirst(0, 2f);
            _localization.SetLanguage(_localization.GetNextLanguage());
            _session.Reset();
            Assert.That(_session.CreateSnapshot().Cubes, Is.Empty);
            Assert.That(_localization.Language, Is.EqualTo("en"));
            Assert.That(_storage.Writes, Is.Zero);
        }

        [Test]
        public void UnsupportedSavedLanguageUsesCurrentLanguageWithoutWriting()
        {
            _storage.Value = new GameProgress("ja", State(new Cube(0, 0f)));
            Assert.That(_progress.TryLoad(_session, _localization, out string error), Is.True);
            Assert.That(error, Is.Null);
            Assert.That(_localization.Language, Is.EqualTo("ru"));
            Assert.That(_storage.Writes, Is.Zero);
        }

        [Test]
        public void AdaptiveDimensionsRestoreAndBottomRemovalPreservesCoordinates()
        {
            _session.ResizePlayfield(14f, 8f);
            _session.PlaceFirst(0, 13.9f);
            _session.AddNext(1);
            Assert.That(_progress.TrySave(_session.CreateSnapshot(), "ru", out _), Is.True);
            GameSession loaded = CreateSession();
            Assert.That(_progress.TryLoad(loaded, _localization, out _), Is.True);
            Assert.That(loaded.CreateSnapshot().PlayfieldWidth, Is.EqualTo(14f));
            loaded.ResizePlayfield(7f, 16f);
            TowerState before = loaded.CreateSnapshot();
            loaded.Remove(0);
            TowerState after = loaded.CreateSnapshot();
            Assert.That(0.5f + after.BaseX * 6f, Is.EqualTo(0.5f + before.BaseX * 6f + before.Cubes[1].HorizontalOffset).Within(0.00001f));
            Assert.That(after.ReferenceWidth, Is.EqualTo(7f));
            Assert.That(after.ReferenceHeight, Is.EqualTo(8f));
        }

        private static GameSession CreateSession() => new GameSession(new GameConfiguration(Enumerable.Repeat(CubeColor.Red, 2).ToArray(), 0.5f, 0.4f, 7f, 8f), new Random(7));
        private static TowerState State(params Cube[] cubes) => new TowerState(7f, 8f, 0.5f, cubes);

        private sealed class TestStorage : IProgressStorage
        {
            public GameProgress Value { get; set; }
            public int Writes { get; private set; }
            public bool FailRead { get; set; }
            public bool FailWrite { get; set; }
            public bool ProgrammingError { get; set; }

            public GameProgress Load()
            {
                if (ProgrammingError)
                {
                    throw new InvalidOperationException();
                }

                if (FailRead)
                {
                    throw new IOException();
                }

                return Value;
            }

            public void Save(GameProgress progress)
            {
                if (ProgrammingError)
                {
                    throw new InvalidOperationException();
                }

                if (FailWrite)
                {
                    throw new IOException();
                }

                Value = progress;
                Writes++;
            }
        }

        private sealed class TestLocalization : IGameLocalization
        {
            public string Language { get; private set; } = "ru";
            public IReadOnlyList<string> AvailableLanguages { get; } = Array.AsReadOnly(new[] { "ru", "en" });
            public event Action Changed;

            public string Get(string key) => key;

            public void SetLanguage(string language)
            {
                Language = language;
                Changed?.Invoke();
            }
        }
    }
}

