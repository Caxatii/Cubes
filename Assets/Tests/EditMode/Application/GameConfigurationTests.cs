using Cubes.Domain.Tower;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Cubes.Application.Configuration;
using Cubes.Application.Gameplay;
using Cubes.Application.Progress;
using NUnit.Framework;

namespace Cubes.Tests.EditMode.Application
{
    public sealed class GameConfigurationTests
    {
        [Test]
        public void ConfigurationStoresTypeCount()
        {
            GameConfiguration configuration = new GameConfiguration(Enumerable.Repeat(CubeColor.Red, 2).ToArray(), 0.5f, 0.4f, 7f, 8f);
            Assert.That(configuration.TypeCount, Is.EqualTo(2));
        }

        [TestCase(0)]
        
        public void NonpositiveTypeCountIsRejected(int count)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameConfiguration(new CubeColor[count], 0.5f, 0.4f, 7f, 8f));
        }

        [Test]
        public void TowerSnapshotCopiesItsInputCollection()
        {
            List<Cube> cubes = new List<Cube> { new Cube(0, 0f) };
            TowerState state = new TowerState(7f, 8f, 0.3f, cubes);
            cubes.Clear();
            Assert.That(state.Cubes.Count, Is.EqualTo(1));
            Assert.Throws<NotSupportedException>(() => ((IList<Cube>)state.Cubes).Clear());
        }

        [Test]
        public void PublicApplicationContractsDoNotExposeUnity()
        {
            Assembly assembly = typeof(GameSession).Assembly;
            foreach (Type type in assembly.GetExportedTypes())
            {
                foreach (ConstructorInfo constructor in type.GetConstructors())
                {
                    foreach (ParameterInfo parameter in constructor.GetParameters())
                    {
                        AssertContractType(parameter.ParameterType);
                    }
                }

                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    AssertContractType(property.PropertyType);
                }

                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    AssertContractType(method.ReturnType);
                    foreach (ParameterInfo parameter in method.GetParameters())
                    {
                        AssertContractType(parameter.ParameterType);
                    }
                }
            }
        }

        private static void AssertContractType(Type type)
        {
            Assert.That(type.Namespace ?? "", Does.Not.StartWith("Unity"));
            foreach (Type argument in type.GetGenericArguments())
            {
                AssertContractType(argument);
            }
        }
    }
}
