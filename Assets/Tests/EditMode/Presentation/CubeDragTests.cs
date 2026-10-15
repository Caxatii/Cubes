using Cubes.Domain.Tower;
using System;
using System.Reflection;
using System.Collections.Generic;
using Cubes.Application.Localization;
using Cubes.Presentation.Notifications;
using Cubes.Application.Gameplay;
using Cubes.Composition.Configuration;
using Cubes.Presentation.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Cubes.Tests.EditMode.Presentation
{
    public sealed class CubeDragTests
    {
        private GameObject _root;
        private GameplayScreen _screen;
        private CubeDrag _drag;
        private CubeView _cube;
        private PointerEventData _pointer;
        private CubeViewFactory _factory;
        private TowerView _tower;
        private int _typeIndex;

        [SetUp]
        public void SetUp()
        {
            GameConfigurationAsset assets = AssetDatabase.LoadAssetAtPath<GameConfigurationAsset>(
                "Assets/Content/Configuration/GameConfiguration.asset");
            int variant = 0;
            _typeIndex = variant;
            _root = new GameObject("Drag test", typeof(RectTransform), typeof(Canvas), typeof(GameplayScreen));
            _screen = _root.GetComponent<GameplayScreen>();
            RectTransform field = CreateRect("Field", _root.transform, new Vector2(800f, 800f));
            BuildAreaView buildArea = field.gameObject.AddComponent<BuildAreaView>();
            RectTransform towerRoot = CreateRect("Tower", field, field.sizeDelta);
            RectTransform dragRoot = CreateRect("Drag", _root.transform, field.sizeDelta);
            DropZoneView dropZone = CreateRect("Hole", _root.transform, new Vector2(100f, 100f))
                .gameObject.AddComponent<DropZoneView>();
            ScrollRect bar = CreateRect("Bar", _root.transform, new Vector2(400f, 180f))
                .gameObject.AddComponent<ScrollRect>();
            bar.viewport = CreateRect("Viewport", bar.transform, new Vector2(400f, 180f));
            bar.content = CreateRect("Content", bar.viewport, new Vector2(1600f, 180f));
            GameControlsView controls = CreateRect("Controls", _root.transform, Vector2.one)
                .gameObject.AddComponent<GameControlsView>();
            GameObject dialog = CreateRect("Dialog", controls.transform, Vector2.one).gameObject;
            dialog.SetActive(false);
            SetField(controls, "_dialog", dialog);
            SetField(_screen, "_buildArea", buildArea);
            SetField(_screen, "_dropZone", dropZone);
            SetField(_screen, "_towerRoot", towerRoot);
            SetField(_screen, "_dragRoot", dragRoot);
            SetField(_screen, "_cubeBar", bar);
            SetField(_screen, "_controls", controls);
            SetField(_screen, "_cubePrefab", AssetDatabase.LoadAssetAtPath<CubeView>("Assets/Content/Prefabs/Cube.prefab"));
            NotificationStackView notifications = CreateRect("Notifications", _root.transform, new Vector2(400f, 600f))
                .gameObject.AddComponent<NotificationStackView>();
            SetField(notifications, "_prefab", AssetDatabase.LoadAssetAtPath<NotificationView>("Assets/Content/Prefabs/Notification.prefab"));
            notifications.Initialize(assets.Notifications, new TestLocalization());
            SetField(_screen, "_notifications", notifications);
            CubeViewFactory factory = new CubeViewFactory(_screen, assets.CubeBar);
            _factory = factory;
            TowerState state = new TowerState(7f, 8f, 0f, new[] { new Cube(variant, 0f) });
            TowerView tower = new TowerView(_screen, factory, assets.Animations, state);
            _tower = tower;
            _drag = new CubeDrag(_screen, tower, factory, assets.Gestures, _root.GetComponent<Canvas>());
            SetField(_screen, "_tower", tower);
            SetField(_screen, "_drag", _drag);
            tower.Rebuild(state);
            tower.Relayout();
            _cube = _screen.FindCubeView(0);
            Canvas.ForceUpdateCanvases();
            SetField(_screen, "_initialized", true);
            SetField(_screen, "_layoutValid", true);
            typeof(GameplayScreen).GetMethod("RememberLayout", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_screen, null);
            _pointer = new PointerEventData(null)
            {
                pointerId = 1,
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, _cube.RectTransform.position)
            };
            _pointer.pressPosition = _pointer.position;
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                _screen.CancelGesture();
                Object.DestroyImmediate(_root);
            }
        }

        [Test]
        public void HorizontalBarGestureScrollsAndAllowsTowerDragAfterRelease()
        {
            ScrollRect bar = _screen.CubeBar;
            CubeView source = _factory.Create(_typeIndex, bar.content, true);
            bar.horizontalNormalizedPosition = 0.5f;
            Canvas.ForceUpdateCanvases();
            Vector2 before = bar.content.anchoredPosition;
            PointerEventData pointer = new PointerEventData(null)
            {
                pointerId = 3,
                position = RectTransformUtility.WorldToScreenPoint(null, source.RectTransform.position)
            };
            pointer.pressPosition = pointer.position;
            source.OnPointerDown(pointer);
            pointer.position += Vector2.right * 100f;
            source.OnDrag(pointer);
            Assert.That(bar.content.anchoredPosition.x, Is.GreaterThan(before.x));
            Assert.That(_screen.DragRoot.childCount, Is.Zero);
            Assert.That(source.transform.parent, Is.EqualTo(bar.content));
            source.OnEndDrag(pointer);
            BeginTowerDrag();
        }

        [Test]
        public void CancelReturnsTowerCubeAndAllowsAnotherDrag()
        {
            BeginTowerDrag();
            _screen.CancelGesture();
            AssertReturned();
            BeginTowerDrag();
            Assert.That(_cube.transform.parent, Is.EqualTo(_screen.DragRoot));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RemovalKeepsSurvivingViewsAndNextDragUsesCurrentIndex(int removedIndex)
        {
            Cube[] cubes = { new Cube(0, 0f), new Cube(1, 0f), new Cube(0, 0f) };
            _tower.Place(new TowerState(7f, 8f, 0f, new[] { cubes[0], cubes[1] }),
                _factory.Create(1, _screen.DragRoot));
            _tower.CompleteMovement();
            _tower.Place(new TowerState(7f, 8f, 0f, cubes), _factory.Create(0, _screen.DragRoot));
            _tower.CompleteMovement();
            CubeView[] views = { _screen.FindCubeView(0), _screen.FindCubeView(1), _screen.FindCubeView(2) };
            Cube[] remaining = new Cube[2];
            int survivorIndex = 0;
            for (int index = 0; index < cubes.Length; index++)
            {
                if (index != removedIndex)
                {
                    remaining[survivorIndex++] = cubes[index];
                }
            }

            _tower.Remove(new TowerState(7f, 8f, 0f, remaining), removedIndex);
            Object.DestroyImmediate(views[removedIndex].gameObject);
            _tower.CompleteMovement();
            survivorIndex = 0;
            for (int index = 0; index < views.Length; index++)
            {
                if (index != removedIndex)
                {
                    Assert.That(_screen.FindCubeView(survivorIndex++), Is.SameAs(views[index]));
                }
            }

            _cube = _screen.FindCubeView(1);
            _pointer.pressPosition = RectTransformUtility.WorldToScreenPoint(null, _cube.RectTransform.position);
            BeginTowerDrag();
            Assert.That(_drag.SourceIndex, Is.EqualTo(1));
            _screen.CancelGesture();
            Assert.That(_screen.FindCubeView(1), Is.SameAs(_cube));
            AssertReturned();
        }

        [Test]
        public void DropWithoutSubscriberReturnsCubeAndAllowsAnotherDrag()
        {
            int removals = 0;
            _screen.RemovalRequested += index => removals++;
            BeginTowerDrag();
            _cube.OnEndDrag(_pointer);
            _tower.CompleteMovement();
            AssertReturned();
            Assert.That(removals, Is.Zero);
            BeginTowerDrag();
            Assert.That(_cube.transform.parent, Is.EqualTo(_screen.DragRoot));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BarDropDispatchesFirstPlacementOrAddition(bool emptyTower)
        {
            if (emptyTower)
            {
                _tower.Remove(new TowerState(7f, 8f, 0f, Array.Empty<Cube>()), 0);
                Object.DestroyImmediate(_cube.gameObject);
            }

            int firstPlacements = 0;
            int additions = 0;
            _screen.FirstPlacementRequested += (type, x) =>
            {
                firstPlacements++;
                Assert.That(type, Is.EqualTo(1));
                Assert.That(x, Is.InRange(0f, 8f));
                _screen.PlaceCube(new TowerState(7f, 8f, 0.5f, new[] { new Cube(type, 0f) }));
            };
            _screen.AdditionRequested += (type, targetIndex) =>
            {
                additions++;
                Assert.That(type, Is.EqualTo(1));
                _screen.PlaceCube(new TowerState(7f, 8f, 0f, new[] { new Cube(0, 0f), new Cube(type, 0f) }));
            };
            CubeView source = _factory.Create(1, _screen.CubeBar.content, true);
            PointerEventData pointer = new PointerEventData(null)
            {
                pointerId = 4,
                position = RectTransformUtility.WorldToScreenPoint(null, source.RectTransform.position)
            };
            pointer.pressPosition = pointer.position;
            source.OnPointerDown(pointer);
            pointer.position += Vector2.up * 100f;
            source.OnDrag(pointer);
            Transform target = emptyTower ? _screen.BuildArea.transform : _cube.transform;
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, target.position);
            source.OnDrag(pointer);
            source.OnEndDrag(pointer);
            _tower.CompleteMovement();
            Assert.That(firstPlacements, Is.EqualTo(emptyTower ? 1 : 0));
            Assert.That(additions, Is.EqualTo(emptyTower ? 0 : 1));
            Assert.That(_screen.FindCubeView(emptyTower ? 0 : 1).TypeIndex, Is.EqualTo(1));
        }

        [Test]
        public void FailedDropHandlerStillReturnsCubeAndReleasesPointer()
        {
            Action<int> handler = request => throw new InvalidOperationException("Drop failed");
            _screen.RemovalRequested += handler;
            BeginTowerDrag();
            _screen.DropZone.RectTransform.position = _cube.RectTransform.position;
            Assert.Throws<InvalidOperationException>(() => _cube.OnEndDrag(_pointer));
            _screen.RemovalRequested -= handler;
            AssertReturned();
            BeginTowerDrag();
            Assert.That(_cube.transform.parent, Is.EqualTo(_screen.DragRoot));
        }

        [Test]
        public void OtherPointerCannotMoveOrFinishDragAndHandledDropKeepsReturnAnimation()
        {
            int drops = 0;
            _screen.RemovalRequested += request =>
            {
                drops++;
                Assert.That(request, Is.EqualTo(0));
                _screen.ReturnCube();
            };
            BeginTowerDrag();
            _screen.DropZone.RectTransform.position = _cube.RectTransform.position;
            Vector2 position = _cube.RectTransform.anchoredPosition;
            PointerEventData other = new PointerEventData(null)
            {
                pointerId = 2,
                position = _pointer.position + Vector2.right * 200f
            };
            _cube.OnDrag(other);
            _cube.OnEndDrag(other);
            Assert.That(_cube.RectTransform.anchoredPosition, Is.EqualTo(position));
            Assert.That(drops, Is.Zero);
            _cube.OnEndDrag(_pointer);
            Assert.That(drops, Is.EqualTo(1));
            Assert.That(_cube.transform.parent, Is.EqualTo(_screen.TowerRoot));
            Assert.That(_screen.IsBusy, Is.True);
            Assert.That(_drag.SourceIndex, Is.Null);
        }

        private void BeginTowerDrag()
        {
            _pointer.position = _pointer.pressPosition;
            _cube.OnPointerDown(_pointer);
            _pointer.position += Vector2.up * 100f;
            _cube.OnDrag(_pointer);
            Assert.That(_cube.transform.parent, Is.EqualTo(_screen.DragRoot));
        }

        private void AssertReturned()
        {
            Assert.That(_cube.transform.parent, Is.EqualTo(_screen.TowerRoot));
            Assert.That(_cube.GetComponent<CanvasGroup>().interactable, Is.True);
            Assert.That(_drag.SourceIndex, Is.Null);
            Assert.That(_screen.DragRoot.childCount, Is.Zero);
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            return rect;
        }

        private sealed class TestLocalization : IGameLocalization
        {
            public string Language => "en";
            public IReadOnlyList<string> AvailableLanguages => new[] { "en" };
            public event Action Changed { add { } remove { } }
            public string Get(string key) => key;
            public void SetLanguage(string language) { }
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
