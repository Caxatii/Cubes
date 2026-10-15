using System;
using System.IO;
using System.Security;
using Cubes.Application.Gameplay;
using Cubes.Application.Localization;

namespace Cubes.Application.Progress
{
    public sealed class ProgressService
    {
        private readonly IProgressStorage _storage;

        public ProgressService(IProgressStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public bool TryLoad(GameSession session, IGameLocalization localization, out string diagnostic)
        {
            diagnostic = null;

            try
            {
                return LoadAvailableProgress(session, localization, out diagnostic);
            }
            catch (Exception exception) when (IsStorageError(exception))
            {
                diagnostic = exception.Message;
                return TryRecoverBackup(session, localization, ref diagnostic);
            }
        }

        private bool LoadAvailableProgress(GameSession session, IGameLocalization localization, out string diagnostic)
        {
            diagnostic = null;
            GameProgress progress = _storage.Load();
            if (progress == null)
            {
                return LoadBackupForMissingPrimary(session, localization, out diagnostic);
            }

            if (string.IsNullOrWhiteSpace(progress.Language))
            {
                throw new InvalidDataException("The saved language is missing.");
            }

            ValidateVersion(progress);
            RestoreProgress(progress, session, localization);
            return true;
        }

        private bool LoadBackupForMissingPrimary(GameSession session, IGameLocalization localization, out string diagnostic)
        {
            diagnostic = null;
            if (!(_storage is IRecoverableProgressStorage recovery) || !(recovery.LoadBackup() is GameProgress backup))
            {
                return false;
            }

            RestoreBackup(backup, session, localization);
            recovery.RejectPrimary();
            diagnostic = "Primary progress missing; recovered the backup.";
            return true;
        }

        private bool TryRecoverBackup(GameSession session, IGameLocalization localization, ref string diagnostic)
        {
            if (!(_storage is IRecoverableProgressStorage recovery))
            {
                return false;
            }

            recovery.RejectPrimary();

            try
            {
                GameProgress backup = recovery.LoadBackup();

                if (backup == null)
                {
                    return false;
                }

                RestoreBackup(backup, session, localization);
                diagnostic = "Primary progress failed; recovered the backup. " + diagnostic;

                return true;
            }
            catch (Exception backupError) when (IsStorageError(backupError))
            {
                diagnostic += " Backup: " + backupError.Message;

                return false;
            }
        }

        private static bool IsStorageError(Exception exception) => exception is ArgumentException
            || exception is OverflowException || exception is IOException || exception is InvalidDataException
            || exception is UnauthorizedAccessException || exception is SecurityException;

        private static void ValidateVersion(GameProgress progress)
        {
            if (progress.FormatVersion < 0 || progress.FormatVersion > GameProgress.CurrentFormatVersion)
                throw new InvalidDataException("Unsupported progress version.");
        }

        private static void RestoreBackup(GameProgress progress, GameSession session, IGameLocalization localization)
        {
            ValidateVersion(progress);
            if (string.IsNullOrWhiteSpace(progress.Language)) throw new InvalidDataException("Missing language.");
            RestoreProgress(progress, session, localization);
        }

        private static void RestoreProgress(GameProgress progress, GameSession session, IGameLocalization localization)
        {
            session.Restore(progress.Tower);
            foreach (string language in localization.AvailableLanguages)
                if (string.Equals(language, progress.Language, StringComparison.Ordinal))
                {
                    localization.SetLanguage(language);
                    break;
                }
        }

        public bool TrySave(TowerState state, string language, out string diagnostic)
        {
            diagnostic = null;
            try
            {
                _storage.Save(new GameProgress(language, state));
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is SecurityException)
            {
                diagnostic = exception.Message;
                return false;
            }
        }
    }
}
