using System;
using Quiz.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Quiz.Gameplay.UI
{
    public class SetScoreWindow : UIElement
    {
        [SerializeField] private TextMeshProUGUI _label = default;
        [SerializeField] private TMP_InputField _inputField = default;

        [SerializeField] private Button _closeButton = default;
        [SerializeField] private Button _setButton = default;

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