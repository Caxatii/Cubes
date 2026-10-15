using System.Collections.Generic;
using Cubes.Presentation.Configuration;
using UnityEngine;
using DG.Tweening;

namespace Cubes.Presentation.Gameplay
{
    internal sealed class CubeEffects
    {
        private readonly HashSet<CubeView> _effects = new HashSet<CubeView>();
        private readonly RectTransform _fallRoot;
        private readonly AnimationConfigurationAsset _animations;

        public CubeEffects(RectTransform fallRoot, AnimationConfigurationAsset animations)
        {
            _fallRoot = fallRoot;
            _animations = animations;
        }

        public void Remove(CubeView view)
        {
            _effects.Add(view);
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_fallRoot, view.RectTransform);
            Rect opening = _fallRoot.rect;

            float scale = bounds.size.x > opening.width && bounds.size.x > 0f
                ? opening.width / bounds.size.x
                : 1f;

            float halfWidth = bounds.extents.x * scale;
            Vector3 entry = bounds.center;
            entry.x = Mathf.Clamp(entry.x, opening.xMin + halfWidth, opening.xMax - halfWidth);
            entry.y = Mathf.Max(entry.y, opening.center.y + bounds.extents.y * scale);
            Vector3 target = view.RectTransform.position + _fallRoot.TransformVector(entry - bounds.center);

            if ((entry - bounds.center).sqrMagnitude > 0.01f || scale < 1f)
            {
                view.EnterHole(target, scale, _animations.DropDuration * 0.25f)
                    .SetUpdate(true).OnComplete(() => Fall(view, _animations.DropDuration * 0.75f));
                return;
            }

            Fall(view, _animations.DropDuration);
        }

        private void Fall(CubeView view, float duration)
        {
            CubeViewFactory.Reparent(view, _fallRoot);
            float distance = Mathf.Max(_animations.DropDistance, _fallRoot.rect.height + view.Size.y);
            view.Fall(distance, duration).SetUpdate(true).OnComplete(() => DestroyEffect(view));
        }

        public void Reject(CubeView view)
        {
            _effects.Add(view);
            view.Reject(_animations).SetUpdate(true).OnComplete(() => DestroyEffect(view));
        }

        public void Clear()
        {
            foreach (CubeView cube in _effects)
            {
                Object.Destroy(cube.gameObject);
            }

            _effects.Clear();
        }

        private void DestroyEffect(CubeView cube)
        {
            _effects.Remove(cube);

            if (cube != null)
            {
                Object.Destroy(cube.gameObject);
            }
        }
    }
}
