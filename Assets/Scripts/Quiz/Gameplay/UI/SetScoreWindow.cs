using System;
using Quiz.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Quiz.Gameplay.UI
{
    public class SetScoreWindow : UIElement
    {
        [SerializeField] private Text _label;
        [SerializeField] private InputField _inputField;

        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _setButton;

        private const string SetScoreText = "Установим очки для {0}!";

        private Action<string> _onScoreSet;
        private string _scoreToSet;

        private void Awake()
        {
            _inputField.onEndEdit.AddListener(arg => { _scoreToSet = arg; });

            _closeButton.onClick.AddListener(() => { gameObject.SetActive(false); });
            _setButton.onClick.AddListener(() =>
            {
                _onScoreSet?.Invoke(_scoreToSet);

                gameObject.SetActive(false);
            });
        }

        public void Show(Player player, Action<string> onScoreSet)
        {
            _label.text = string.Format(SetScoreText, player.Name);
            _onScoreSet = onScoreSet;
            _inputField.text = player.Points.ToString();

            ShowGameObject();
        }
    }
}