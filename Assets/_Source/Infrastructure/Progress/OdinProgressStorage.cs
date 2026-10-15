using System;
using System.IO;
using Cubes.Application.Progress;
using Cubes.Domain.Tower;
using OdinSerializer;

namespace Cubes.Infrastructure.Progress
{
    public sealed class OdinProgressStorage : IRecoverableProgressStorage
    {
        private readonly string _path;
        private bool _primaryRejected;

        public OdinProgressStorage(string path) => _path = Path.GetFullPath(path);
        public GameProgress Load() => Read(_path);
        public GameProgress LoadBackup() => Read(_path + ".bak");
        public void RejectPrimary() => _primaryRejected = true;

        private static GameProgress Read(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                DeserializationContext context = new DeserializationContext();
                context.Config.SerializationPolicy = SerializationPolicies.Everything;
                context.Config.DebugContext.ErrorHandlingPolicy = ErrorHandlingPolicy.ThrowOnErrors;
                context.Config.DebugContext.LoggingPolicy = LoggingPolicy.Silent;
                GameProgress progress = SerializationUtility.DeserializeValue<GameProgress>(File.ReadAllBytes(path), DataFormat.Binary, context);
                Validate(progress);
                return progress;
            }
            catch (SerializationAbortException exception)
            {
                throw new InvalidDataException("Invalid saved progress.", exception);
            }
            catch (InvalidOperationException exception)
            {
                throw new InvalidDataException("Invalid binary progress stream.", exception);
            }
        }

        private static void Validate(GameProgress progress)
        {
            if (progress?.Tower?.Cubes == null || string.IsNullOrWhiteSpace(progress.Language)
                || progress.FormatVersion < 0 || progress.FormatVersion > GameProgress.CurrentFormatVersion)
                throw new InvalidDataException("Invalid or unsupported saved progress.");

            var state = progress.Tower;

            if (float.IsNaN(state.ReferenceWidth) || float.IsInfinity(state.ReferenceWidth) || state.ReferenceWidth < 1f
                || float.IsNaN(state.ReferenceHeight) || float.IsInfinity(state.ReferenceHeight) || state.ReferenceHeight < 1f)
                throw new InvalidDataException("Invalid reference dimensions.");

            try
            {
                foreach (Cube cube in state.Cubes)
                    if (cube == null || cube.TypeIndex < 0)
                        throw new InvalidDataException("Invalid cube type.");

                new TowerModel(new TowerSettings(0.5f, 0.4f), new PlayfieldSettings(state.PlayfieldWidth, state.PlayfieldHeight))
                    .Restore(state.Cubes, state.BaseX);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("Invalid saved tower.", exception);
            }
        }

        public void Save(GameProgress progress)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));

            Validate(progress);

            SerializationContext context = new SerializationContext();
            context.Config.SerializationPolicy = SerializationPolicies.Everything;
            context.Config.DebugContext.ErrorHandlingPolicy = ErrorHandlingPolicy.ThrowOnErrors;
            context.Config.DebugContext.LoggingPolicy = LoggingPolicy.Silent;

            byte[] bytes = SerializationUtility.SerializeValue(progress, DataFormat.Binary, context);
            Directory.CreateDirectory(Path.GetDirectoryName(_path));

            string temporary = _path + ".tmp";

            using (FileStream stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            if (!File.Exists(_path))
            {
                File.Move(temporary, _path);
            }
            else
            {
                bool preservePrimary = !_primaryRejected;

                if (preservePrimary)
                {
                    try { Read(_path); }
                    catch (InvalidDataException) { preservePrimary = false; }
                }

                try
                {
                    File.Replace(temporary, _path, preservePrimary ?
                        _path + ".bak" : null);
                }
                catch (PlatformNotSupportedException)
                {
                    if (preservePrimary)
                        File.Copy(_path, _path + ".bak", true);

                    File.Delete(_path);
                    File.Move(temporary, _path);
                }
            }

            _primaryRejected = false;
        }
    }
}
