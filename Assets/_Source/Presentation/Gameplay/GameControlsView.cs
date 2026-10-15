using System;
using Cubes.Application.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cubes.Presentation.Gameplay
{
    public sealed class GameControlsView : MonoBehaviour
    {
        [SerializeField] private Button _languageButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TMP_Text _languageLabel;
        [SerializeField] private TMP_Text _resetLabel;
        [SerializeField] private TMP_Text _questionLabel;
        [SerializeField] private TMP_Text _confirmLabel;
        [SerializeField] private TMP_Text _cancelLabel;
        [SerializeField] private GameObject _dialog;

        private IGameLocalization _localization;

        public bool DialogOpen => _dialog.activeSelf;
        public Button ResetButton => _resetButton;
        public event Action LanguageRequested;
        public event Action ResetConfirmed;

        public void Initialize(IGameLocalization localization)
        {
            ReleaseBindings();
            _localization = localization;
            _localization.Changed += Refresh;
            _languageButton.onClick.AddListener(RequestLanguageChange);
            _confirmButton.onClick.AddListener(Confirm);
            _cancelButton.onClick.AddListener(CloseDialog);
            CloseDialog();
            Refresh();
        }

        public void CloseDialog()
        {
            _dialog.SetActive(false);
        }

        private void RequestLanguageChange()
        {
            LanguageRequested?.Invoke();
        }

        public void OpenDialog()
        {
            _dialog.SetActive(true);
        }

        private void Confirm()
        {
            CloseDialog();
            ResetConfirmed?.Invoke();
        }

        private void Refresh()
        {
            _languageLabel.text = _localization.GetNextLanguage().ToUpperInvariant();
            _resetLabel.text = _localization.Get(LocalizationKeys.ResetButton);
            _questionLabel.text = _localization.Get(LocalizationKeys.ResetQuestion);
            _confirmLabel.text = _localization.Get(LocalizationKeys.Confirm);
            _cancelLabel.text = _localization.Get(LocalizationKeys.Cancel);
        }

        private void OnDestroy() => ReleaseBindings();

        private void ReleaseBindings()
        {
            if (_localization != null)
            {
                _localization.Changed -= Refresh;
                _languageButton.onClick.RemoveListener(RequestLanguageChange);
                _confirmButton.onClick.RemoveListener(Confirm);
                _cancelButton.onClick.RemoveListener(CloseDialog);
            }
        }
    }
}
