using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Quiz.Gameplay.UI
{
    public class DecideAnswerWindow : UIElement
    {
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private CanvasGroup[] _canvasGroups;

        [SerializeField] private Button _correctButton;
        [SerializeField] private Button _incorrectButton;

        private Action<float> _onDecided;

        private void Awake()
        {
            _correctButton.onClick.AddListener(() =>
            {
                if (float.TryParse(_inputField.text, out var multiplier))
                    _onDecided?.Invoke(multiplier);
            });

            _incorrectButton.onClick.AddListener(() => { _onDecided?.Invoke(-1); });
        }

        public void Show(Action<float> onDecided)
        {
            _onDecided = onDecided;
        }

        private void Update()
        {
            foreach (var image in _canvasGroups)
            {
                var mousePosition = Input.mousePosition;
                var imagePosition = image.transform.position;

                var positionDiff = mousePosition.x > imagePosition.x
                    ? (mousePosition - imagePosition).x
                    : (imagePosition - mousePosition).x;

                image.alpha = (255 - positionDiff) / 255;
            }
        }
    }
}