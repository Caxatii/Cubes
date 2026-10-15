using System;
using Cubes.Application.Gameplay;
using Cubes.Application.Localization;
using Cubes.Presentation.Configuration;
using Cubes.Presentation.Notifications;
using UnityEngine;
using UnityEngine.UI;

namespace Cubes.Presentation.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayScreen : MonoBehaviour
    {
        [SerializeField] private BuildAreaView _buildArea;
        [SerializeField] private DropZoneView _dropZone;
        [SerializeField] private RectTransform _towerRoot;
        [SerializeField] private RectTransform _dragRoot;
        [SerializeField] private ScrollRect _cubeBar;
        [SerializeField] private CubeView _cubePrefab;
        [SerializeField] private GameControlsView _controls;
        [SerializeField] private NotificationStackView _notifications;
        [SerializeField] private RectTransform _fallRoot;

        private TowerView _tower;
        private CubeDrag _drag;
        private CubeEffects _effects;
        private GameSession _session;
        private Rect _lastBounds;
        private Vector2Int _lastScreenSize;
        private Matrix4x4 _lastFieldMatrix;
        private bool _layoutDirty;
        private bool _layoutValid;
        private bool _initialized;

        public BuildAreaView BuildArea => _buildArea;
        public DropZoneView DropZone => _dropZone;
        public RectTransform TowerRoot => _towerRoot;
        public RectTransform DragRoot => _dragRoot;
        public ScrollRect CubeBar => _cubeBar;
        public CubeView CubePrefab => _cubePrefab;
        public GameControlsView Controls => _controls;
        public bool IsBusy => _tower != null && _tower.IsBusy;
        internal CubeDrag Drag => _drag;
        public event Action<int, float> FirstPlacementRequested;
        public event Action<int, int> AdditionRequested;
        public event Action<int> RemovalRequested;

        public void Initialize(CubeBarAppearanceAsset appearance, GameSession session,
            AnimationConfigurationAsset animations, GestureConfigurationAsset gestures,
            NotificationConfigurationAsset notifications, IGameLocalization localization)
        {
            if (_initialized)
            {
                throw new InvalidOperationException("The gameplay screen is already initialized.");
            }

            ValidateReferences();
            _session = session ?? throw new ArgumentNullException(nameof(session));
            Canvas canvas = GetComponentInParent<Canvas>();
            _controls.Initialize(localization);
            _notifications.Initialize(notifications, localization);
            CubeViewFactory factory = new CubeViewFactory(this, appearance);
            TowerState state = _session.CreateSnapshot();
            _tower = new TowerView(this, factory, animations, state);
            _drag = new CubeDrag(this, _tower, factory, gestures, canvas);
            _effects = new CubeEffects(_fallRoot, animations);
            factory.BuildCubeBar();
            Canvas.ForceUpdateCanvases();
            CubeBar.horizontalNormalizedPosition = 0f;
            _initialized = true;
            RebuildTower(state);
        }

        public void PlaceCube(TowerState state) => _tower.Place(state, _drag.Take());

        public void RemoveCube(TowerState state, int index)
        {
            CubeView removed = _drag.Take();
            _tower.Remove(state, index);
            _effects.Remove(removed);
        }

        public void RejectCube() => _effects.Reject(_drag.Take());

        public void ReturnCube()
        {
            _tower.Return(_drag.Take(), true);
        }

        public void RebuildTower(TowerState state)
        {
            CancelGesture();
            _effects.Clear();
            _tower.Rebuild(state);
            RebuildLayout();
        }

        public void RebuildLayout()
        {
            if (!_initialized)
            {
                return;
            }

            CancelGesture();
            _tower.CompleteMovement();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
            Canvas.ForceUpdateCanvases();

            try
            {
                TowerLayout layout = new TowerLayout(_tower.State, BuildArea.RectTransform.rect, CubePrefab.Size);
                Vector2 logicalSize = layout.LogicalSize;
                Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
                Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                Vector2 fieldCenter = RectTransformUtility.WorldToScreenPoint(camera, BuildArea.RectTransform.position);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(BuildArea.RectTransform,
                    new Vector2(fieldCenter.x, Screen.height), camera, out Vector2 screenTop))
                    throw new InvalidOperationException("Cannot resolve the top of the screen.");
                float height = layout.ToLogical(screenTop).y;
                bool wasLimited = _session.IsHeightLimitReached;
                TowerState state = _session.ResizePlayfield(logicalSize.x, height);
                if (!wasLimited && _session.IsHeightLimitReached) Notify(LocalizationKeys.LimitReached);
                _tower.Relayout(state);
                _notifications.Relayout();
                _layoutValid = true;
            }
            catch (InvalidOperationException exception)
            {
                _layoutValid = false;
                Debug.LogWarning(exception.Message);
                Notify(LocalizationKeys.LayoutError);
            }

            RememberLayout();
        }

        public CubeView FindCubeView(int index) => _tower.Cubes[index];
        public void Notify(string key) => _notifications.Show(key);
        public void ResetNotifications() => _notifications.Clear();

        public void CancelGesture()
        {
            _drag?.Cancel();
            CubeBar.StopMovement();
        }

        private void LateUpdate() => RefreshLayout();

        private bool RefreshLayout()
        {
            if (!_initialized)
            {
                return false;
            }

            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

            if (!_layoutDirty && screenSize == _lastScreenSize
                && BuildArea.RectTransform.rect == _lastBounds
                && BuildArea.RectTransform.localToWorldMatrix == _lastFieldMatrix)
            {
                return false;
            }

            RebuildLayout();
            return true;
        }

        private void OnRectTransformDimensionsChange() => _layoutDirty = true;

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                Suspend();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Suspend();
            }
        }

        private void OnDisable() => Suspend();

        private void OnDestroy()
        {
            Suspend();
            _effects?.Clear();
        }

        private void Suspend()
        {
            if (_initialized)
            {
                CancelGesture();
                _tower.CompleteMovement();
            }
        }

        internal bool CanCapture(bool fromBar) => _initialized && !_controls.DialogOpen && (fromBar || !IsBusy);

        internal bool RefreshInteractionLayout() => !RefreshLayout() && _layoutValid;

        internal void SubmitFirstPlacement(int typeIndex, float x) => FirstPlacementRequested?.Invoke(typeIndex, x);

        internal void SubmitAddition(int typeIndex, int targetIndex) => AdditionRequested?.Invoke(typeIndex, targetIndex);

        internal void SubmitRemoval(int index) => RemovalRequested?.Invoke(index);

        private void RememberLayout()
        {
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastBounds = BuildArea.RectTransform.rect;
            _lastFieldMatrix = BuildArea.RectTransform.localToWorldMatrix;
            _layoutDirty = false;
        }

        private void ValidateReferences()
        {
            if (_buildArea == null || _dropZone == null || _towerRoot == null || _dragRoot == null
                || _cubeBar == null || _cubePrefab == null
                || _controls == null || _notifications == null || _fallRoot == null
                || _cubeBar.content == null || _cubeBar.viewport == null)
            {
                throw new InvalidOperationException("Gameplay screen references are incomplete.");
            }
        }
    }
}
