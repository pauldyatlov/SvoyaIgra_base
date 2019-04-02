using System;
using System.Collections.Generic;
using System.Linq;
using Quiz.Network;
using UnityEngine;

namespace Quiz.Gameplay.UI
{
    // ReSharper disable once InconsistentNaming
    public class UIController : MonoBehaviour
    {
        [SerializeField] private RoundScreen _roundScreenTemplate = default;
        [SerializeField] private RectTransform _roundContainer = default;

        [SerializeField] private PlayerScore _playerScoreTemplate = default;
        [SerializeField] private RectTransform _scoreContainer = default;

        [SerializeField] private TaskScreen _taskScreen = default;
        [SerializeField] private SetScoreWindow _setScoreWindow = default;

        public event Action<Player> PlayerAnswering;

        public readonly Dictionary<Player, PlayerScore> PlayerViews = new Dictionary<Player, PlayerScore>();
        private readonly Dictionary<int, RoundScreen> _rounds = new Dictionary<int, RoundScreen>();
        private Action<Player> _onPlayerKicked;

        private int _currentRound;
        private Player _decisionMaker;

        public void Show(Plan plan, Action<Player> onPlayerKicked)
        {
            _onPlayerKicked = onPlayerKicked;

            SocketServer.OnPlayerAnswered += OnPlayerAnswering;

            for (var i = 0; i < plan.RoundsList.Count; i++)
            {
                var round = plan.RoundsList[i];
                var roundScreen = Instantiate(_roundScreenTemplate, _roundContainer);
                roundScreen.Show(round, question => { _taskScreen.Show(question, this); },
                    RemoveRound);

                if (i == 0)
                    roundScreen.ShowGameObject();

                _rounds.Add(i, roundScreen);
            }

            _currentRound = 0;
        }

        private void RemoveRound(RoundPlan round)
        {
            _rounds[_currentRound].HideGameObject();
            _rounds.Remove(_currentRound);

            _currentRound++;
            _rounds[_currentRound].ShowGameObject();

            SetDecisionMaker(PlayerViews.OrderBy(x => x.Key.Points).FirstOrDefault().Key);
        }

        private void OnPlayerSelected(Player player)
        {
            _setScoreWindow.Show(player, score =>
            {
                int.TryParse(score, out var intScore);

                player.SetPoints(intScore);
            });
        }

        public void PlayerDisconnected(KeyValuePair<string, Player> player)
        {
            var view = PlayerViews.FirstOrDefault(x => x.Key.Name == player.Key);

            if (view.Value == null)
            {
                Debug.LogError("Can't find view for " + player.Key);
                return;
            }

            view.Value.HideGameObject();

            PlayerViews.Remove(view.Key);
        }

        public void NewPlayerConnected(Player player)
        {
            var scorePanel = Instantiate(_playerScoreTemplate, _scoreContainer);
            scorePanel.Show(player, OnPlayerSelected, _onPlayerKicked);

            PlayerViews.Add(player, scorePanel);
        }

        private void OnPlayerAnswering(string playerName)
        {
            var player = PlayerViews.FirstOrDefault(x => x.Key.Name == playerName);

            if (player.Value == null)
            {
                Debug.LogError("Cannot find view to set as answering");
                return;
            }

            PlayerAnswering?.Invoke(player.Key);
        }

        public void SetDecisionMaker(Player player)
        {
            if (_decisionMaker == player)
                return;

            _decisionMaker?.OnSetAsDecisionMaker?.Invoke(false);

            _decisionMaker = player;
            _decisionMaker.OnSetAsDecisionMaker?.Invoke(true);
        }
    }
}