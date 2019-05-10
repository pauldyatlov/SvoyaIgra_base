using System;
using System.Collections.Generic;
using System.Linq;
using Quiz.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Quiz.Gameplay.UI
{
    // ReSharper disable once InconsistentNaming
    public class UIController : UIElement
    {
        [SerializeField] private SetRoomScreen _setRoomScreen = default;
        [SerializeField] private RoundScreen _roundScreenTemplate = default;
        [SerializeField] private RectTransform _roundContainer = default;

        [SerializeField] private PlayerScore _playerScoreTemplate = default;
        [SerializeField] private RectTransform _scoreContainer = default;

        [SerializeField] private TaskScreen _taskScreen = default;
        [SerializeField] private FinalRoundScreen _finalRoundScreen = default;

        [SerializeField] private SetScoreWindow _setScoreWindow = default;
        [SerializeField] private Button _gameInfoButton = default;
        [SerializeField] private GameInfoWindow _gameInfoWindow = default;

        public event Action<Player> PlayerAnswering;
        public QuestionReader QuestionReader;

        public readonly Dictionary<Player, PlayerScore> PlayerViews = new Dictionary<Player, PlayerScore>();
        private readonly Dictionary<int, RoundScreen> _rounds = new Dictionary<int, RoundScreen>();
        private Action<Player> _onPlayerKicked;

        private Plan _plan;
        private int _currentRound;
        private Player _decisionMaker;
        private string _selectedRoom;

        private void Awake()
        {
            _gameInfoButton.onClick.AddListener(() =>
            {
                if (_gameInfoWindow.gameObject.activeSelf)
                {
                    _gameInfoWindow.Close();
                    return;
                }

                _gameInfoWindow.Show(_selectedRoom);
            });
        }

        public void Show(Action<string> onRoomSelected)
        {
            QuestionReader = new QuestionReader();

            ShowGameObject();

            _setRoomScreen.Show(roomName =>
            {
                _selectedRoom = roomName;

                onRoomSelected(roomName);
            });
        }

        public void StartGame(Plan plan, Action<Player> onPlayerKicked)
        {
            _plan = plan;
            _onPlayerKicked = onPlayerKicked;

            SocketServer.OnPlayerAnswered += OnPlayerAnswering;

            for (var i = 0; i < plan.RoundsList.Count; i++)
            {
                var round = plan.RoundsList[i];
                var roundScreen = Instantiate(_roundScreenTemplate, _roundContainer);
                roundScreen.Show(i, this, round, question => { _taskScreen.Show(question, this); },
                    RemoveRound);

                if (i == 0)
                    roundScreen.ShowGameObject();

                _rounds.Add(i, roundScreen);
            }
        }

        private void RemoveRound(RoundPlan round)
        {
            _rounds[_currentRound].HideGameObject();
            _rounds.Remove(_currentRound);

            _currentRound++;

            if (_currentRound < _plan.RoundsList.Count)
                _rounds[_currentRound].ShowGameObject();
            else
                _finalRoundScreen.Show(_plan.FinalQuestions);
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

        public void SetDecisionMaker()
        {
            var smallestCountPlayer = PlayerViews.OrderBy(x => x.Key.Points).FirstOrDefault().Key;

            if (smallestCountPlayer == null)
                return;

            var smallestCount = smallestCountPlayer.Points;
            var sameCountPlayers = PlayerViews.Where(x => x.Key.Points == smallestCount && x.Key.Online).ToArray();

            if (sameCountPlayers.Length > 1)
            {
                SetDecisionMaker(sameCountPlayers.PickRandom().Key);
                return;
            }

            SetDecisionMaker(smallestCountPlayer);
        }

        public void SetDecisionMaker(Player player)
        {
            if (_decisionMaker == player)
                return;

            _decisionMaker?.OnSetAsDecisionMaker?.Invoke(false);

            _decisionMaker = player;
            _decisionMaker.OnSetAsDecisionMaker?.Invoke(true);
        }

        private void OnDisable()
        {
            QuestionReader.Close();
        }
    }
}