using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cubes.Presentation.Configuration;

namespace Cubes.Presentation.Notifications
{
    [RequireComponent(typeof(RectMask2D))]
    public sealed class NotificationView : MonoBehaviour
    {
        private const float MaximumWidth = 760f;
        private const float MinimumHeight = 64f;
        private const float HorizontalPadding = 40f;
        private const float VerticalPadding = 24f;
        private const float EntryOffset = 24f;
        private const float EntryScale = 0.8f;

        [SerializeField] private TMP_Text _label;
        [SerializeField] private RectTransform _content;
        [SerializeField] private CanvasGroup _group;

        private Tween _life;
        private Tween _shift;
        private Action _closed;

        private RectTransform _rectTransform;

        public RectTransform RectTransform => _rectTransform != null ? _rectTransform : _rectTransform = (RectTransform)transform;
        public string Key { get; private set; }
        public float Height => RectTransform.rect.height;

        public void SetText(string key, string text, float availableWidth)
        {
            Key = key;
            _label.text = text;
            float width = Mathf.Min(MaximumWidth, availableWidth);
            float height = Mathf.Max(MinimumHeight, _label.GetPreferredValues(text, Mathf.Max(1f, width - HorizontalPadding), 0f).y + VerticalPadding);
            RectTransform.sizeDelta = new Vector2(width, height);
        }

        public void Show(NotificationConfigurationAsset settings, Action closed)
        {
            _closed = closed;
            _group.alpha = 0f;
            _content.localScale = Vector3.one * EntryScale;
            _content.anchoredPosition = new Vector2(0f, EntryOffset);
            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(_group.DOFade(1f, settings.EnterDuration));
            sequence.Join(_content.DOScale(1f, settings.EnterDuration).SetEase(Ease.OutBack));
            sequence.Join(_content.DOAnchorPos(Vector2.zero, settings.EnterDuration));
            sequence.AppendInterval(settings.DisplayDuration);
            sequence.Append(_group.DOFade(0f, settings.ExitDuration));
            sequence.OnComplete(() => _closed?.Invoke());
            _life = sequence;
        }

        public void Move(float y, float duration)
        {
            _shift?.Kill();
            _shift = RectTransform.DOAnchorPos(new Vector2(0f, y), duration).SetUpdate(true).SetEase(Ease.OutQuad);
        }

        public void CompleteMove()
        {
            _shift?.Complete();
        }

        public void Hide(NotificationConfigurationAsset settings)
        {
            _life?.Kill();
            _life = _group.DOFade(0f, settings.ExitDuration).SetUpdate(true).OnComplete(() => _closed?.Invoke());
        }

        private void OnDestroy()
        {
            _life?.Kill();
            _shift?.Kill();
        }
    }
}
