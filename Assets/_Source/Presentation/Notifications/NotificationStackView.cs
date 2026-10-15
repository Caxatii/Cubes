using System.Collections.Generic;
using Cubes.Application.Localization;
using Cubes.Presentation.Configuration;
using UnityEngine;

namespace Cubes.Presentation.Notifications
{
    public sealed class NotificationStackView : MonoBehaviour
    {
        [SerializeField] private NotificationView _prefab;

        private readonly List<NotificationView> _active = new List<NotificationView>();
        private readonly List<NotificationView> _closing = new List<NotificationView>();
        private RectTransform _rectTransform;
        private NotificationConfigurationAsset _settings;
        private IGameLocalization _localization;

        public void Initialize(NotificationConfigurationAsset settings, IGameLocalization localization)
        {
            if (_localization != null)
            {
                _localization.Changed -= Relayout;
            }

            _rectTransform = (RectTransform)transform;
            _settings = settings;
            _localization = localization;
            _localization.Changed += Relayout;
        }

        public void Show(string key)
        {
            foreach (NotificationView active in _active)
            {
                active.CompleteMove();
            }

            foreach (NotificationView closing in _closing)
            {
                closing.CompleteMove();
            }

            if (_active.Count >= _settings.MaximumCount)
            {
                NotificationView oldest = _active[_active.Count - 1];
                _active.RemoveAt(_active.Count - 1);
                _closing.Add(oldest);
                oldest.Move(_rectTransform.rect.height + oldest.Height, _settings.ExitDuration);
                oldest.Hide(_settings);
            }

            NotificationView view = Instantiate(_prefab, transform);
            view.SetText(key, _localization.Get(key), _rectTransform.rect.width);
            view.RectTransform.anchoredPosition = new Vector2(0f, -view.Height - _settings.Spacing);
            _active.Insert(0, view);
            view.Show(_settings, () => Remove(view));
            Arrange();
        }

        public void Clear()
        {
            foreach (NotificationView view in _active)
            {
                Destroy(view.gameObject);
            }

            foreach (NotificationView view in _closing)
            {
                Destroy(view.gameObject);
            }

            _active.Clear();
            _closing.Clear();
        }

        private void Remove(NotificationView view)
        {
            _active.Remove(view);
            _closing.Remove(view);

            if (view != null)
            {
                Destroy(view.gameObject);
            }

            Arrange();
        }

        public void Relayout()
        {
            foreach (NotificationView view in _active)
            {
                view.SetText(view.Key, _localization.Get(view.Key), _rectTransform.rect.width);
            }

            Arrange();
        }

        private void Arrange()
        {
            float y = 0f;

            foreach (NotificationView view in _active)
            {
                view.Move(y, _settings.ShiftDuration);
                y += view.Height + _settings.Spacing;
            }
        }

        private void OnDestroy()
        {
            if (_localization != null)
            {
                _localization.Changed -= Relayout;
            }
        }
    }
}
