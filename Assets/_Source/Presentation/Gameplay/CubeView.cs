using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using Cubes.Presentation.Configuration;

namespace Cubes.Presentation.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Image), typeof(CanvasGroup))]
    public sealed class CubeView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, ICancelHandler, ILayoutElement
    {
        [SerializeField] private Image _image;
        [SerializeField] private CanvasGroup _canvasGroup;

        private Tween _animation;
        private CubeDrag _drag;

        private RectTransform _rectTransform;

        public RectTransform RectTransform => _rectTransform != null ? _rectTransform : _rectTransform = (RectTransform)transform;
        public int TypeIndex { get; private set; }
        public bool FromBar { get; private set; }

        public Vector2 Size => RectTransform.rect.size;
        float ILayoutElement.minWidth => -1f;
        float ILayoutElement.minHeight => -1f;
        float ILayoutElement.preferredWidth => Size.x;
        float ILayoutElement.preferredHeight => Size.y;
        float ILayoutElement.flexibleWidth => 0f;
        float ILayoutElement.flexibleHeight => 0f;
        int ILayoutElement.layoutPriority => 1;

        void ILayoutElement.CalculateLayoutInputHorizontal() { }
        void ILayoutElement.CalculateLayoutInputVertical() { }

        internal void Bind(CubeDrag drag, bool fromBar, int typeIndex)
        {
            _drag = drag;
            FromBar = fromBar;
            TypeIndex = typeIndex;
        }

        public void OnPointerDown(PointerEventData data) => _drag?.OnPointerDown(this, data);
        public void OnPointerUp(PointerEventData data) => _drag?.OnPointerUp(this, data);
        public void OnDrag(PointerEventData data) => _drag?.OnDrag(this, data);
        public void OnEndDrag(PointerEventData data) => _drag?.OnEndDrag(this, data);
        public void OnCancel(BaseEventData data) => _drag?.Cancel();
        public void OnInitializePotentialDrag(PointerEventData data) => data.useDragThreshold = false;

        public void OnBeginDrag(PointerEventData data)
        {
        }

        public void SetAppearance(Sprite sprite)
        {
            _image.sprite = sprite;
        }

        public void SetInteractable(bool interactable)
        {
            _image.raycastTarget = interactable;
            _canvasGroup.blocksRaycasts = interactable;
            _canvasGroup.interactable = interactable;
        }

        public Tween MoveTo(Vector2 position, float duration)
        {
            _animation?.Kill();
            _animation = RectTransform.DOAnchorPos(position, duration).SetEase(Ease.OutQuad);
            return _animation;
        }

        public Tween PlaceTo(Vector2 position, Vector3 targetScale, AnimationConfigurationAsset settings)
        {
            _animation?.Kill();
            Vector2 start = RectTransform.anchoredPosition;
            float flight = settings.PlacementDuration;
            Vector3 stretched = Vector3.Scale(targetScale, new Vector3(1f - settings.PlacementStretch, 1f + settings.PlacementStretch, 1f));
            float arc = settings.BounceHeight * targetScale.y;
            Sequence sequence = DOTween.Sequence();

            sequence.Append(DOVirtual.Float(0f, 1f, flight, progress =>
            {
                RectTransform.anchoredPosition = Vector2.LerpUnclamped(start, position, progress)
                    + Vector2.up * (Mathf.Sin(progress * Mathf.PI) * arc);
            }).SetEase(Ease.InQuad));

            sequence.Join(RectTransform.DOScale(stretched, flight).SetEase(Ease.OutQuad));
            AppendLanding(sequence, position, targetScale, settings.PlacementSquash,
                Size.y * targetScale.y * settings.PlacementBounce, settings.PlacementSettleDuration);

            _animation = sequence;

            return sequence;
        }

        public Tween SettleTo(Vector2 position, AnimationConfigurationAsset settings, Vector3? targetScale = null)
        {
            _animation?.Kill();
            Vector3 scale = targetScale ?? RectTransform.localScale;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(RectTransform.DOAnchorPos(position, settings.CollapseDuration).SetEase(Ease.InQuad));

            if (targetScale.HasValue)
            {
                sequence.Join(RectTransform.DOScale(scale, settings.CollapseDuration));
            }

            AppendLanding(sequence, position, scale, settings.CollapseSquash,
                settings.CollapseBounceHeight, settings.CollapseSettleDuration);
            _animation = sequence;

            return sequence;
        }

        private void AppendLanding(Sequence sequence, Vector2 position, Vector3 scale, float squash, float bounce, float duration)
        {
            Vector3 compressed = Vector3.Scale(scale, new Vector3(1f + squash, 1f - squash, 1f));
            Vector2 contact = position - Vector2.up * (Size.y * scale.y * squash * 0.5f);
            sequence.Append(RectTransform.DOScale(compressed, duration * 0.25f).SetEase(Ease.OutQuad));
            sequence.Join(RectTransform.DOAnchorPos(contact, duration * 0.25f).SetEase(Ease.OutQuad));
            sequence.Append(RectTransform.DOScale(scale, duration * 0.35f).SetEase(Ease.OutQuad));
            sequence.Join(RectTransform.DOAnchorPos(position + Vector2.up * bounce, duration * 0.35f).SetEase(Ease.OutQuad));
            sequence.Append(RectTransform.DOAnchorPos(position, duration * 0.4f).SetEase(Ease.InQuad));

            sequence.AppendCallback(() =>
            {
                RectTransform.anchoredPosition = position;
                RectTransform.localScale = scale;
            });
        }

        public Tween Reject(AnimationConfigurationAsset settings)
        {
            _animation?.Kill();
            Sequence sequence = DOTween.Sequence();
            sequence.Append(RectTransform.DOScale(RectTransform.localScale * settings.MissPeakScale, settings.MissExpandDuration));
            sequence.Append(RectTransform.DOScale(Vector3.zero, settings.MissShrinkDuration));
            _animation = sequence;

            return sequence;
        }

        public Tween EnterHole(Vector3 position, float scaleFactor, float duration)
        {
            _animation?.Kill();
            Sequence sequence = DOTween.Sequence();
            sequence.Append(RectTransform.DOMove(position, duration).SetEase(Ease.OutQuad));
            sequence.Join(RectTransform.DOScale(RectTransform.localScale * scaleFactor, duration).SetEase(Ease.OutQuad));
            _animation = sequence;

            return sequence;
        }

        public Tween Fall(float distance, float duration)
        {
            _animation?.Kill();
            _animation = RectTransform.DOAnchorPosY(RectTransform.anchoredPosition.y - distance, duration).SetEase(Ease.InQuad);

            return _animation;
        }

        private void OnDestroy()
        {
            _animation?.Kill();
        }
    }
}
