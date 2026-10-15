using Cubes.Domain.Tower;
using System.Collections.Generic;
using Cubes.Application.Gameplay;
using Cubes.Presentation.Configuration;
using DG.Tweening;
using UnityEngine;

namespace Cubes.Presentation.Gameplay
{
    internal sealed class TowerView
    {
        private readonly GameplayScreen _screen;
        private readonly CubeViewFactory _factory;
        private readonly AnimationConfigurationAsset _animations;
        private readonly List<CubeView> _cubes = new List<CubeView>();
        private Tween _movement;
        private TowerLayout _layout;

        public TowerView(GameplayScreen screen, CubeViewFactory factory, AnimationConfigurationAsset animations, TowerState state)
        {
            _screen = screen;
            _factory = factory;
            _animations = animations;
            State = state;
        }

        public TowerState State { get; private set; }
        public IReadOnlyList<CubeView> Cubes => _cubes;
        public bool IsBusy { get; private set; }

        private void UpdateLayout()
        {
            _layout = new TowerLayout(State, _screen.BuildArea.RectTransform.rect, _factory.PrefabSize);
        }

        public void Place(TowerState state, CubeView view)
        {
            State = state;
            UpdateLayout();
            int index = _cubes.Count;
            Cube cube = State.Cubes[index];
            _cubes.Add(view);
            CubeViewFactory.Reparent(view, _screen.TowerRoot);
            _factory.Bind(view, false, cube.TypeIndex);
            Vector3 initialScale = view.RectTransform.localScale;
            _factory.MatchTowerScale(view, _layout.CubeSize);
            Vector3 targetScale = view.RectTransform.localScale;
            view.RectTransform.localScale = initialScale;
            bool first = state.Cubes.Count == 1;
            SetBusy(true);

            Tween placement = first
                ? view.SettleTo(GetCubePosition(index), _animations, targetScale)
                : view.PlaceTo(GetCubePosition(index), targetScale, _animations);

            _movement = placement
                .SetUpdate(true).OnComplete(() => SetBusy(false));
        }

        public void Remove(TowerState state, int removedIndex)
        {
            State = state;
            UpdateLayout();
            _cubes.RemoveAt(removedIndex);

            if (_cubes.Count == 0)
            {
                return;
            }

            SetBusy(true);
            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            int firstFallingIndex = -1;
            bool moved = false;

            for (int index = 0; index < state.Cubes.Count; index++)
            {
                CubeView view = _cubes[index];
                Vector2 target = GetCubePosition(index);
                Vector2 current = view.RectTransform.anchoredPosition;
                if ((current - target).sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                bool falling = current.y > target.y + 0.01f;
                if (falling && firstFallingIndex < 0)
                {
                    firstFallingIndex = index;
                }

                float delay = falling ? (index - firstFallingIndex) * _animations.CollapseStagger : 0f;
                sequence.Insert(delay, falling ? view.SettleTo(target, _animations) : view.MoveTo(target, _animations.CollapseDuration));
                moved = true;
            }

            if (!moved)
            {
                sequence.Kill();
                SetBusy(false);
                return;
            }

            _movement = sequence.OnComplete(() => SetBusy(false));
        }

        public void Return(CubeView view, bool animate)
        {
            int index = _cubes.IndexOf(view);
            CubeViewFactory.Reparent(view, _screen.TowerRoot);

            if (animate)
            {
                SetBusy(true);
                _movement = view.MoveTo(GetCubePosition(index), _animations.ReturnDuration)
                    .SetUpdate(true).OnComplete(() => SetBusy(false));
            }
            else
            {
                view.RectTransform.anchoredPosition = GetCubePosition(index);
                view.SetInteractable(true);
            }
        }

        public void Rebuild(TowerState state)
        {
            _movement?.Kill();

            foreach (CubeView cube in _cubes)
            {
                UnityEngine.Object.Destroy(cube.gameObject);
            }

            _cubes.Clear();
            State = state;

            foreach (Cube cube in state.Cubes)
            {
                CubeView view = _factory.Create(cube.TypeIndex, _screen.TowerRoot);
                _cubes.Add(view);
            }

            SetBusy(false);
        }

        public void Relayout(TowerState state = null)
        {
            CompleteMovement();
            State = state ?? State;
            UpdateLayout();
            Vector2 size = _layout.CubeSize;

            for (int index = 0; index < _cubes.Count; index++)
            {
                _factory.MatchTowerScale(_cubes[index], size);
                _cubes[index].RectTransform.anchoredPosition = GetCubePosition(index);
            }
        }

        public void CompleteMovement() => _movement?.Complete(true);

        public int IndexOf(CubeView view) => _cubes.IndexOf(view);

        private Vector2 GetCubePosition(int index)
        {
            Vector2 field = _layout.GetCubePosition(index);
            return (Vector2)_screen.TowerRoot.InverseTransformPoint(_screen.BuildArea.RectTransform.TransformPoint(field))
                - _screen.TowerRoot.rect.center;
        }

        private void SetBusy(bool busy)
        {
            IsBusy = busy;

            foreach (CubeView view in _cubes)
            {
                view.SetInteractable(!busy);
            }
        }
    }
}
