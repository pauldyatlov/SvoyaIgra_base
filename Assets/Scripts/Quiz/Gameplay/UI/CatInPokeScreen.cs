using System;
using System.Collections.Generic;
using Quiz.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Quiz.Gameplay.UI
{
    public class CatInPokeScreen : UIElement
    {
        [SerializeField] private Text _label = default;

        [SerializeField] private RectTransform _container = default;
        [SerializeField] private PlayerScore _playerScoreTemplate = default;

        private readonly List<PlayerScore> _playersList = new List<PlayerScore>();

        private Action<Player> _callback;

        public void Show(List<Player> players, CatInPoke question, Action<Player> callback)
        {
            gameObject.SetActive(true);

            _callback = callback;

            _label.text = "\'" + question.Theme + "\', " + question.Price;

            foreach (var player in players)
            {
                var stat = Instantiate(_playerScoreTemplate, _container, false);
                stat.Show(player, _callback, null);

                _playersList.Add(stat);
            }
        }

        public void Close()
        {
            gameObject.SetActive(false);

            foreach (var player in _playersList)
                Destroy(player.gameObject);

            _playersList.Clear();
        }
    }
}