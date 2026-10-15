using System;
using System.IO;
using Cubes.Application.Configuration;
using Cubes.Application.Gameplay;
using Cubes.Application.Localization;
using Cubes.Application.Progress;
using Cubes.Composition.Configuration;
using Cubes.Infrastructure.Localization;
using Cubes.Infrastructure.Progress;
using Cubes.Presentation.Gameplay;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Cubes.Composition.Gameplay
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameConfigurationAsset _configuration;
        [SerializeField] private GameplayScreen _screen;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IGameConfigurationSource>(resolver => resolver.Resolve<GameConfigurationAsset>(), Lifetime.Singleton);
            builder.Register(resolver => resolver.Resolve<IGameConfigurationSource>().CreateConfiguration(), Lifetime.Singleton);
            builder.Register<AnyCubePlacementRule>(Lifetime.Singleton).As<ICubePlacementRule>();
            builder.RegisterInstance(_configuration);
            builder.RegisterInstance(_screen);
            builder.Register<ProgressService>(Lifetime.Singleton);
            builder.Register(resolver => new GameSession(resolver.Resolve<GameConfiguration>(),
                new System.Random(), resolver.Resolve<ICubePlacementRule>()), Lifetime.Singleton);
            builder.Register<GameplayPresenter>(Lifetime.Singleton).AsSelf().As<IDisposable>();
            builder.RegisterInstance<IProgressStorage>(new OdinProgressStorage(
                Path.Combine(UnityEngine.Application.persistentDataPath, "progress.odin")));
            builder.Register<GameLocalization>(Lifetime.Singleton).As<IGameLocalization>().As<IDisposable>();
            builder.RegisterEntryPoint<GameBootstrap>();
        }
    }
}
