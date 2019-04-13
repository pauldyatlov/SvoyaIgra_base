using System.Collections.Generic;
using System.Globalization;
using Quiz.Network;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Quiz.Gameplay.UI
{
    public class TaskScreen : UIElement
    {
        [SerializeField] private GameObject _themesGameObject = default;
        [SerializeField] private AudioSource _audioSource = default;
        [SerializeField] private StreamVideo _videoPlayer = default;

        [SerializeField] private TimerPanel _questionTimerPanel = default;
        [SerializeField] private TimerPanel _playerTimerPanel = default;

        [SerializeField] private TimerCounter _timerCounter = default;

        [SerializeField] private CatInPokeScreen _catInPokeScreen = default;
        [SerializeField] private Text _answeringPlayerLabel = default;
        [SerializeField] private Text _label = default;
        [SerializeField] private Image _image = default;

        [SerializeField] private Button _acceptButton = default;
        [SerializeField] private Button _declineButton = default;
        [SerializeField] private Button _canAnswerButton = default;

        private readonly List<Player> _failedPlayers = new List<Player>();
        private bool _paused;

        private QuestionPlan _plane;
        private Player _answeringPlayer;

        private const int MinTimer = 12;
        private const int PlayerAnswerTimer = 10;

        private bool _canAcceptAnswers;
        private Player _catInPokePlayer;
        private UIController _uiController;

        private int QuestionPrice => _plane.CatInPoke.Price > 0 ? _plane.CatInPoke.Price : _plane.Price;
        private readonly List<Player> _countedPlayers = new List<Player>();

        private QuizTimer _questionTimer;
        private QuizTimer _playerTimer;

        private void Awake()
        {
            _acceptButton.onClick.AddListener(CorrectAnswer);
            _declineButton.onClick.AddListener(IncorrectAnswer);

            _canAnswerButton.onClick.AddListener(CanAnswerHandler);
        }

        public void Show(QuestionPlan plan, UIController controller)
        {
            _canAcceptAnswers = false;

            _plane = plan;
            _uiController = controller;

            _questionTimer = new QuizTimer(MinTimer, _questionTimerPanel, true, new[]
            {
                new QuizThreshold(3, () => { SoundManager.Instance.PlayNoAnswer(); }),
                new QuizThreshold(0, Close)
            });

            if (plan.IsCatInPoke)
            {
                _catInPokePlayer = null;

                _catInPokeScreen.Show(
                    _uiController.PlayerViews.Keys.ToList(),
                    plan.CatInPoke,
                    player =>
                    {
                        Display();
                        _catInPokeScreen.Close();

                        _catInPokePlayer = player;
                        _answeringPlayer = null;

                        ShowQuestion(plan);
                    });
            }
            else
            {
                Display();
                ShowQuestion(plan);
            }
        }

        private void Display()
        {
            _themesGameObject.SetActive(false);

            ShowGameObject();

            _acceptButton.gameObject.SetActive(false);
            _declineButton.gameObject.SetActive(false);
            _canAnswerButton.gameObject.SetActive(true);
        }

        private void ShowQuestion(QuestionPlan plan)
        {
            _failedPlayers.Clear();

            _label.text = _plane.Question;
            _image.gameObject.SetActive(plan.Picture != null);

            if (plan.Picture != null)
                _image.sprite = plan.Picture;

            _videoPlayer.gameObject.SetActive(_plane.Video != null);

            if (_plane.Video != null)
                _videoPlayer.Show(_plane.Video);

            _answeringPlayerLabel.text = "";
            _answeringPlayer = null;

            _timerCounter.Show();
            _timerCounter.RunTimer(_questionTimer);

            _uiController.PlayerAnswering += HandlePlayerAnswering;

            if (plan.Picture != null)
                CanAnswerHandler();
        }

        private void HandlePlayerAnswering(Player player)
        {
            if (!_canAcceptAnswers)
                return;

            _countedPlayers.Add(player);

            foreach (var idlePlayer in _uiController.PlayerViews)
            {
                if (_countedPlayers.Contains(idlePlayer.Key))
                {
                    idlePlayer.Value.SetCountdown(false);
                    idlePlayer.Value.LatencyLabelVisible(true);

                    continue;
                }

                if (_answeringPlayer == null)
                    idlePlayer.Value.SetCountdown(true);
            }

            if (_answeringPlayer != null || _failedPlayers.Contains(player))
                return;

            _playerTimer = new QuizTimer(PlayerAnswerTimer, _playerTimerPanel, false, new []
            {
                new QuizThreshold(0, IncorrectAnswer),
            });

            _timerCounter.RunTimer(_playerTimer);

            _acceptButton.gameObject.SetActive(true);
            _declineButton.gameObject.SetActive(true);

            _questionTimer.Pause();
            _audioSource.Pause();

            _answeringPlayer = player;
            SetAsAnswering(_answeringPlayer, true);
        }

        private void CanAnswerHandler()
        {
            _questionTimer.Unpause();

            _canAnswerButton.gameObject.SetActive(false);

            if (_plane.Audio != null)
                _audioSource.PlayOneShot(_plane.Audio);

            _canAcceptAnswers = true;

            if (_plane.IsCatInPoke && _catInPokePlayer != null)
                HandlePlayerAnswering(_catInPokePlayer);
        }

        private void CorrectAnswer()
        {
            SetAsAnswering(_answeringPlayer, false);

            _answeringPlayer.UpdatePoints(QuestionPrice);
            _uiController.SetDecisionMaker(_answeringPlayer);

            _questionTimer.Unpause();
            _audioSource.UnPause();

            _answeringPlayer.SendMessage(new QuizCommand
            {
                Command = SocketServer.CorrectAnswer,
                Parameter = QuestionPrice.ToString()
            });

            _playerTimer?.Stop();
            _playerTimer = null;

            _answeringPlayer = null;

            Close();
        }

        private void IncorrectAnswer()
        {
            ClearCounter();

            _failedPlayers.Add(_answeringPlayer);

            SetAsAnswering(_answeringPlayer, false);
            _answeringPlayer.UpdatePoints(-QuestionPrice);

            _questionTimer.Unpause();
            _audioSource.UnPause();

            _acceptButton.gameObject.SetActive(false);
            _declineButton.gameObject.SetActive(false);

            _answeringPlayer.SendMessage(new QuizCommand
            {
                Command = SocketServer.WrongAnswer,
                Parameter = (-QuestionPrice).ToString()
            });

            _playerTimer?.Stop();
            _playerTimer = null;

            _answeringPlayer = null;
        }

        private void SetAsAnswering(Player player, bool value)
        {
            _answeringPlayerLabel.text = value ? "ОТВЕЧАЕТ " + player.Name : "";

            foreach (var view in _uiController.PlayerViews.Where(x => !_failedPlayers.Contains(x.Key)))
                view.Value.SetCanvasGroup(!value);

            var registeredPlayer = _uiController.PlayerViews.FirstOrDefault(x => x.Key == player);

            if (registeredPlayer.Value != null)
                registeredPlayer.Value.SetCanvasGroup(value);
        }

        private void ClearCounter()
        {
            foreach (var idlePlayer in _uiController.PlayerViews)
            {
                idlePlayer.Value.SetCountdown(false);
                idlePlayer.Value.LatencyLabelVisible(false);
            }
        }

        public override void Close()
        {
            ClearCounter();
            _countedPlayers.Clear();

            if (_answeringPlayer != null)
                IncorrectAnswer();

            foreach (var view in _uiController.PlayerViews)
                view.Value.SetCanvasGroup(true);

            _uiController.PlayerAnswering -= HandlePlayerAnswering;

            _themesGameObject.SetActive(true);
            _timerCounter.Close();

            _playerTimer?.Stop();
            _playerTimer = null;

            base.Close();
        }
    }
}