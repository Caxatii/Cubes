using Cubes.Application.Gameplay;
using Cubes.Presentation.Configuration;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cubes.Presentation.Gameplay
{
    internal sealed class CubeDrag
    {
        private enum DragMode
        {
            Pending,
            ScrollBar,
            DragCube,
            Rejected
        }

        private readonly GameplayScreen _screen;
        private readonly TowerView _tower;
        private readonly CubeViewFactory _factory;
        private readonly GestureConfigurationAsset _settings;
        private readonly Canvas _canvas;
        private CubeView _owner;
        private int _pointerId;
        private DragMode _mode;
        private Vector2 _start;
        private CubeView _dragged;
        private Vector2 _pointerOffset;

        public CubeDrag(GameplayScreen screen, TowerView tower, CubeViewFactory factory,
            GestureConfigurationAsset settings, Canvas canvas)
        {
            _screen = screen;
            _tower = tower;
            _factory = factory;
            _settings = settings;
            _canvas = canvas;
        }

        public int? SourceIndex
        {
            get
            {
                int index = _dragged != null && _owner != null && !_owner.FromBar ? _tower.IndexOf(_dragged) : -1;
                return index >= 0 ? (int?)index : null;
            }
        }

        public void OnPointerDown(CubeView source, PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || _owner != null || !_screen.CanCapture(source.FromBar))
            {
                return;
            }

            _owner = source;
            _pointerId = data.pointerId;
            _start = data.position;
            _mode = DragMode.Pending;

            if (source.FromBar)
            {
                _screen.CubeBar.OnInitializePotentialDrag(data);
            }
        }

        public void OnDrag(CubeView source, PointerEventData data)
        {
            if (_owner != source || data.pointerId != _pointerId)
            {
                return;
            }

            if (_mode == DragMode.Pending)
            {
                ResolveDragMode(data);
            }

            if (_mode == DragMode.ScrollBar)
            {
                _screen.CubeBar.OnDrag(data);
            }
            else if (_mode == DragMode.DragCube)
            {
                Move(data.position, data.pressEventCamera);
            }
        }

        public void OnPointerUp(CubeView source, PointerEventData data)
        {
            if (_owner == source && data.pointerId == _pointerId && (_mode == DragMode.Pending || _mode == DragMode.Rejected))
            {
                ClearGesture();
            }
        }

        public void OnEndDrag(CubeView source, PointerEventData data)
        {
            if (_owner != source || data.pointerId != _pointerId)
            {
                return;
            }

            try
            {
                if (_mode == DragMode.DragCube && _screen.RefreshInteractionLayout())
                {
                    SubmitDrop(data.pressEventCamera);
                }
            }
            finally
            {
                // A handled drop transfers the view through Take; otherwise return it to its source.
                Cancel();
            }
        }

        private void ResolveDragMode(PointerEventData data)
        {
            Vector2 delta = data.position - _start;
            float threshold = _settings.DragThreshold * _canvas.scaleFactor;

            if (delta.sqrMagnitude < threshold * threshold)
            {
                return;
            }

            if (!_owner.FromBar || (delta.y > 0f && Mathf.Abs(delta.y) >= Mathf.Abs(delta.x)))
            {
                CubeView source = _owner;
                bool began = Begin(source, data);

                if (_owner == source)
                {
                    _mode = began ? DragMode.DragCube : DragMode.Rejected;
                }

                return;
            }

            Vector2 current = data.position;
            data.position = _start;
            _screen.CubeBar.OnBeginDrag(data);
            data.position = current;
            _mode = DragMode.ScrollBar;
        }

        private bool Begin(CubeView source, PointerEventData data)
        {
            if (!_screen.RefreshInteractionLayout() || _screen.IsBusy || _screen.Controls.DialogOpen)
            {
                return false;
            }

            _screen.CubeBar.StopMovement();
            int index = _tower.IndexOf(source);

            if (!source.FromBar && index < 0)
            {
                return false;
            }

            Vector2 sourceCenter = RectTransformUtility.WorldToScreenPoint(data.pressEventCamera, source.RectTransform.position);
            _pointerOffset = sourceCenter - data.pressPosition;
            _dragged = source.FromBar ? _factory.Create(source.TypeIndex, _screen.DragRoot) : source;
            Vector3 sourceScale = source.RectTransform.lossyScale;
            CubeViewFactory.Reparent(_dragged, _screen.DragRoot);
            Vector3 parentScale = _screen.DragRoot.lossyScale;
            _dragged.RectTransform.localScale = new Vector3(sourceScale.x / parentScale.x, sourceScale.y / parentScale.y, 1f);
            _dragged.SetInteractable(false);
            Move(data.position, data.pressEventCamera);

            return true;
        }

        private void Move(Vector2 pointerPosition, Camera camera)
        {
            if (_dragged == null)
            {
                return;
            }

            Vector2 center = pointerPosition + _pointerOffset;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_screen.DragRoot, center, camera, out Vector2 local))
            {
                _dragged.RectTransform.anchoredPosition = local - _screen.DragRoot.rect.center;
            }

            _screen.DropZone.SetPointerPosition(center, camera);
        }

        private void SubmitDrop(Camera camera)
        {
            if (_dragged == null)
            {
                return;
            }

            Vector2 center = RectTransformUtility.WorldToScreenPoint(camera, _dragged.RectTransform.position);

            if (!_owner.FromBar)
            {
                if (_screen.DropZone.Contains(center, camera))
                {
                    _screen.SubmitRemoval(_tower.IndexOf(_dragged));
                }
                else
                {
                    _screen.ReturnCube();
                    _screen.Notify(LocalizationKeys.Returned);
                }

                return;
            }

            int targetIndex = -1;

            for (int index = 0; index < _tower.Cubes.Count; index++)
            {
                CubeView view = _tower.Cubes[index];

                if (view != _dragged && RectTransformUtility.RectangleContainsScreenPoint(view.RectTransform, center, camera))
                {
                    targetIndex = index;
                    break;
                }
            }

            bool valid = _screen.BuildArea.TryGetLogicalPosition(_tower.State, _factory.PrefabSize, center, camera, out Vector2 logical);
            _screen.DropZone.ClearPointer();

            if (_tower.State.Cubes.Count == 0 && valid && targetIndex < 0)
            {
                _screen.SubmitFirstPlacement(_dragged.TypeIndex, logical.x);
            }
            else if (_tower.State.Cubes.Count > 0 && targetIndex >= 0)
            {
                _screen.SubmitAddition(_dragged.TypeIndex, targetIndex);
            }
            else
            {
                _screen.RejectCube();
                _screen.Notify(LocalizationKeys.Missed);
            }
        }

        public CubeView Take()
        {
            CubeView view = _dragged;
            _dragged = null;
            return view;
        }

        public void Cancel()
        {
            if (_dragged != null)
            {
                int? sourceIndex = SourceIndex;
                if (sourceIndex.HasValue)
                {
                    _tower.Return(_dragged, false);
                }
                else
                {
                    Object.Destroy(_dragged.gameObject);
                }
            }

            Take();
            _screen.DropZone.ClearPointer();
            ClearGesture();
        }

        private void ClearGesture()
        {
            if (_mode == DragMode.ScrollBar)
            {
                _screen.CubeBar.OnEndDrag(new PointerEventData(EventSystem.current));
            }

            _owner = null;
            _pointerId = 0;
            _mode = DragMode.Pending;
        }
    }
}
