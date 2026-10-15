using System;
using Cubes.Application.Gameplay;
using Cubes.Application.Localization;
using Cubes.Application.Progress;
using Cubes.Domain.Tower;
using UnityEngine;

namespace Cubes.Presentation.Gameplay
{
    public sealed class GameplayPresenter : IDisposable
    {
        private readonly GameSession _session;
        private readonly GameplayScreen _screen;
        private readonly ProgressService _progress;
        private readonly IGameLocalization _localization;
        private bool _initialized;

        public GameplayPresenter(GameSession session, GameplayScreen screen, ProgressService progress, IGameLocalization localization)
        {
            _session = session;
            _screen = screen;
            _progress = progress;
            _localization = localization;
        }

        public void Initialize(string loadError)
        {
            if (_initialized)
            {
                throw new InvalidOperationException("The gameplay presenter is already initialized.");
            }

            _screen.FirstPlacementRequested += PlaceFirst;
            _screen.AdditionRequested += AddNext;
            _screen.RemovalRequested += Remove;
            _screen.Controls.LanguageRequested += ChangeLanguage;
            _screen.Controls.ResetButton.onClick.AddListener(OpenResetDialog);
            _screen.Controls.ResetConfirmed += Reset;
            _initialized = true;

            ReportError(loadError, LocalizationKeys.LoadError);
        }

        public void Dispose()
        {
            if (!_initialized)
            {
                return;
            }

            _screen.CancelGesture();
            _screen.FirstPlacementRequested -= PlaceFirst;
            _screen.AdditionRequested -= AddNext;
            _screen.RemovalRequested -= Remove;
            _screen.Controls.LanguageRequested -= ChangeLanguage;
            _screen.Controls.ResetButton.onClick.RemoveListener(OpenResetDialog);
            _screen.Controls.ResetConfirmed -= Reset;
            _initialized = false;
        }

        private void OpenResetDialog()
        {
            _screen.CancelGesture();
            _screen.Controls.OpenDialog();
        }

        private void PlaceFirst(int typeIndex, float x)
        {
            _session.PlaceFirst(typeIndex, x);
            CompletePlacement();
        }

        private void AddNext(int typeIndex, int targetIndex)
        {
            TowerAddResult result = _session.AddNext(typeIndex, targetIndex);

            if (result == TowerAddResult.Added)
            {
                CompletePlacement();
                return;
            }

            _screen.RejectCube();
            _screen.Notify(result == TowerAddResult.LimitReached ? LocalizationKeys.LimitReached
                : result == TowerAddResult.ColorMismatch ? LocalizationKeys.ColorMismatch : LocalizationKeys.Missed);
        }

        private void CompletePlacement()
        {
            TowerState state = _session.CreateSnapshot();
            string error = Save(state);
            _screen.PlaceCube(state);
            _screen.Notify(LocalizationKeys.Placed);
            if (_session.IsHeightLimitReached) _screen.Notify(LocalizationKeys.LimitReached);
            ReportError(error, LocalizationKeys.SaveError);
        }

        private void Remove(int index)
        {
            if (!_session.Remove(index))
            {
                _screen.CancelGesture();
                _screen.Notify(LocalizationKeys.Missed);
                return;
            }

            TowerState state = _session.CreateSnapshot();
            string error = Save(state);
            _screen.RemoveCube(state, index);
            _screen.Notify(LocalizationKeys.Removed);
            ReportError(error, LocalizationKeys.SaveError);
        }

        private void ChangeLanguage()
        {
            _localization.SetLanguage(_localization.GetNextLanguage());
            ReportError(Save(_session.CreateSnapshot()), LocalizationKeys.SaveError);
        }

        private void Reset()
        {
            _screen.CancelGesture();
            _session.Reset();
            TowerState state = _session.CreateSnapshot();
            string error = Save(state);
            _screen.RebuildTower(state);
            _screen.ResetNotifications();
            _screen.Notify(LocalizationKeys.ResetCompleted);
            ReportError(error, LocalizationKeys.SaveError);
        }

        private string Save(TowerState state)
        {
            _progress.TrySave(state, _localization.Language, out string diagnostic);
            return diagnostic;
        }

        private void ReportError(string diagnostic, string notification)
        {
            if (!string.IsNullOrEmpty(diagnostic))
            {
                Debug.LogWarning(diagnostic);
                _screen.Notify(notification);
            }
        }
    }
}
