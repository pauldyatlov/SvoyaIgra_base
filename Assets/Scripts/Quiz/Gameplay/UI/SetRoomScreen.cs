using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Quiz.Gameplay.UI
{
    public class SetRoomScreen : UIElement
    {
        [SerializeField] private TMP_InputField _inputField = default;
        [SerializeField] private Button _saveButton = default;

        private Action<string> _onRoomChosen;

        private void Awake()
        {
            _saveButton.onClick.AddListener(() =>
            {
                _onRoomChosen?.Invoke(_inputField.text);

                Close();
            });
        }

        public void Show(Action<string> onRoomSelected)
        {
            ShowGameObject();

            _onRoomChosen = onRoomSelected;
        }
    }
}