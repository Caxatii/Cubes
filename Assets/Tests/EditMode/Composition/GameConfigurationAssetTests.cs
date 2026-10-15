using System;
using System.Collections.Generic;
using Cubes.Application.Configuration;
using Cubes.Infrastructure.Configuration;
using Cubes.Composition.Configuration;
using Cubes.Presentation.Configuration;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cubes.Tests.EditMode.Composition
{
    public sealed class GameConfigurationAssetTests
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _created.Count - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(_created[index]);
            }

            _created.Clear();
        }

        [Test]
        public void ProjectConfigurationResolvesMigratedAssets()
        {
            GameConfigurationAsset asset = AssetDatabase.LoadAssetAtPath<GameConfigurationAsset>("Assets/Content/Configuration/GameConfiguration.asset");
            Assert.That(asset, Is.Not.Null);
            Assert.DoesNotThrow(() => asset.CreateConfiguration());
            Assert.That(asset.CreateConfiguration().TypeCount, Is.EqualTo(20));
            Assert.That(asset.CubeBar.GetSprite(0), Is.Not.SameAs(asset.CubeBar.GetSprite(1)));
            Assert.That(asset.CreateConfiguration().GetColor(0), Is.EqualTo(asset.CreateConfiguration().GetColor(1)));
        }

        [TestCase(0)]
        [TestCase(999)]
        public void InvalidInspectorColorIsRejected(int color)
        {
            GameConfigurationAsset asset = CreateAsset();
            SetSprites(asset, new[] { CreateSprite() });
            SerializedObject serialized = new SerializedObject(asset.CubeBar);
            serialized.FindProperty("_cubes").GetArrayElementAtIndex(0).FindPropertyRelative("_color").intValue = color;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => asset.CreateConfiguration());
        }

        [Test]
        public void VisualCatalogMustMatchGameplaySource()
        {
            GameConfigurationAsset asset = CreateAsset();
            SetSprites(asset, new[] { CreateSprite() });
            var differentColor = new GameConfiguration(new[] { Cubes.Domain.Tower.CubeColor.Blue }, 0.5f, 0.4f, 7f, 8f);
            Assert.Throws<InvalidOperationException>(() => asset.CubeBar.ValidateCatalog(differentColor));
            var differentCount = new GameConfiguration(new[] { Cubes.Domain.Tower.CubeColor.Red, Cubes.Domain.Tower.CubeColor.Red }, 0.5f, 0.4f, 7f, 8f);
            Assert.Throws<InvalidOperationException>(() => asset.CubeBar.ValidateCatalog(differentCount));
        }

        [TestCase("_cubeBar")]
        [TestCase("_tower")]
        [TestCase("_gestures")]
        [TestCase("_animations")]
        [TestCase("_notifications")]
        public void MissingSectionIsRejectedBeforeCreation(string field)
        {
            GameConfigurationAsset asset = CreateAsset();
            SetSprites(asset, new[] { CreateSprite() });
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty(field).objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => asset.CreateConfiguration());
        }

        [Test]
        public void MissingSpritesAreRejectedBeforeCreation()
        {
            GameConfigurationAsset asset = CreateAsset();
            Assert.Throws<InvalidOperationException>(() => asset.CreateConfiguration());
            SetSprites(asset, new Sprite[] { null });
            Assert.Throws<InvalidOperationException>(() => asset.CreateConfiguration());
        }

        [Test]
        public void RepeatedSpriteIsAllowed()
        {
            GameConfigurationAsset asset = CreateAsset();
            Sprite sprite = CreateSprite();
            SetSprites(asset, new[] { sprite, sprite });
            GameConfigurationAsset source = asset;
            GameConfiguration configuration = source.CreateConfiguration();
            Assert.That(configuration.TypeCount, Is.EqualTo(2));
        }

        [Test]
        public void InvalidInspectorHeightIsRejectedBySource()
        {
            GameConfigurationAsset asset = CreateAsset();
            SetSprites(asset, new[] { CreateSprite() });
            SerializedObject serialized = new SerializedObject(asset.Tower);
            serialized.FindProperty("_playfieldHeight").floatValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<ArgumentOutOfRangeException>(() => asset.CreateConfiguration());
        }

        [TestCase("_placementDuration", -1f)]
        [TestCase("_missPeakScale", 0.9f)]
        [TestCase("_dropDistance", 0f)]
        public void InvalidAnimationSettingsAreRejectedWithoutDataCopies(string field, float value)
        {
            GameConfigurationAsset asset = CreateAsset();
            SetSprites(asset, new[] { CreateSprite() });
            SerializedObject serialized = new SerializedObject(asset.Animations);
            serialized.FindProperty(field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<ArgumentOutOfRangeException>(() => asset.CreateConfiguration());
        }

        [Test]
        public void InvalidNotificationSettingsAreRejectedWithoutDataCopies()
        {
            GameConfigurationAsset asset = CreateAsset();
            SetSprites(asset, new[] { CreateSprite() });
            SerializedObject serialized = new SerializedObject(asset.Notifications);
            serialized.FindProperty("_maximumNotificationCount").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<ArgumentOutOfRangeException>(() => asset.CreateConfiguration());
            serialized.FindProperty("_maximumNotificationCount").intValue = 3;
            serialized.FindProperty("_notificationDisplayDuration").floatValue = 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<ArgumentOutOfRangeException>(() => asset.CreateConfiguration());
        }

        [Test]
        public void InvalidGestureThresholdIsRejectedWithoutDataCopies()
        {
            GameConfigurationAsset asset = CreateAsset();
            SetSprites(asset, new[] { CreateSprite() });
            SerializedObject serialized = new SerializedObject(asset.Gestures);
            serialized.FindProperty("_dragThreshold").floatValue = float.NaN;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<ArgumentOutOfRangeException>(() => asset.CreateConfiguration());
        }

        [Test]
        public void SpriteOrderDefinesTypeIndices()
        {
            GameConfigurationAsset asset = CreateAsset();
            Sprite first = CreateSprite();
            Sprite second = CreateSprite();
            SetSprites(asset, new[] { first, second });
            Assert.That(asset.CreateConfiguration().TypeCount, Is.EqualTo(2));
            Assert.That(asset.CubeBar.GetSprite(0), Is.SameAs(first));
            Assert.That(asset.CubeBar.GetSprite(1), Is.SameAs(second));
            SetSprites(asset, new[] { second, first });
            Assert.That(asset.CubeBar.GetSprite(0), Is.SameAs(second));
            Assert.That(asset.CubeBar.GetSprite(1), Is.SameAs(first));
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void InvalidSpriteIndexIsRejected(int index)
        {
            GameConfigurationAsset asset = CreateAsset();
            SetSprites(asset, new[] { CreateSprite() });
            Assert.Throws<ArgumentOutOfRangeException>(() => asset.CubeBar.GetSprite(index));
        }

        private GameConfigurationAsset CreateAsset()
        {
            GameConfigurationAsset asset = ScriptableObject.CreateInstance<GameConfigurationAsset>();
            _created.Add(asset);
            SerializedObject serialized = new SerializedObject(asset);
            SetSection<CubeBarAppearanceAsset>(serialized, "_cubeBar");
            SetSection<TowerConfigurationAsset>(serialized, "_tower");
            SetSection<GestureConfigurationAsset>(serialized, "_gestures");
            SetSection<AnimationConfigurationAsset>(serialized, "_animations");
            SetSection<NotificationConfigurationAsset>(serialized, "_notifications");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private void SetSection<T>(SerializedObject serialized, string field) where T : ScriptableObject
        {
            T section = ScriptableObject.CreateInstance<T>();
            _created.Add(section);
            serialized.FindProperty(field).objectReferenceValue = section;
        }

        private Sprite CreateSprite()
        {
            Texture2D texture = new Texture2D(2, 2);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
            sprite.name = "red";
            _created.Add(texture);
            _created.Add(sprite);
            return sprite;
        }

        private static void SetSprites(GameConfigurationAsset asset, Sprite[] sprites)
        {
            SerializedObject serialized = new SerializedObject(asset.CubeBar);
            SerializedProperty list = serialized.FindProperty("_cubes");
            list.arraySize = sprites.Length;
            for (int index = 0; index < sprites.Length; index++)
            {
                list.GetArrayElementAtIndex(index).FindPropertyRelative("_sprite").objectReferenceValue = sprites[index];
                list.GetArrayElementAtIndex(index).FindPropertyRelative("_color").intValue = (int)Cubes.Domain.Tower.CubeColor.Red;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
