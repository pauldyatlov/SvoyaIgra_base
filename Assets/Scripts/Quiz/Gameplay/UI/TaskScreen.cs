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
        [SerializeField] private Button _extraPointsAcceptButton = default;
        [SerializeField] private Button _lessPointsAcceptButton = default;

        [SerializeField] private Button _declineButton = default;
        [SerializeField] private Button _canAnswerButton = default;

        private readonly List<Player> _failedPlayers = new List<Player>();
        private bool _paused;

        private QuestionPlan _plan;
        private Player _answeringPlayer;

        private const int MinTimer = 15;
        private const int PlayerAnswerTimer = 20;

        private bool _canAcceptAnswers;
        private Player _catInPokePlayer;
        private UIController _uiController;

        private int QuestionPrice => _plan.CatInPoke.Price > 0 ? _plan.CatInPoke.Price : _plan.Price;
        private readonly List<Player> _countedPlayers = new List<Player>();

        private QuizTimer _questionTimer;
        private QuizTimer _playerTimer;

        private void Awake()
        {
            _acceptButton.onClick.AddListener(() => { CorrectAnswer(); });
            _extraPointsAcceptButton.onClick.AddListener(() => { CorrectAnswer(1.5f); });
            _lessPointsAcceptButton.onClick.AddListener(() => { CorrectAnswer(0.5f); });

            _declineButton.onClick.AddListener(IncorrectAnswer);

            _canAnswerButton.onClick.AddListener(CanAnswerHandler);
        }

        public void Show(QuestionPlan plan, UIController controller)
        {
            _canAcceptAnswers = false;

            _plan = plan;
            _uiController = controller;

            _questionTimer = new QuizTimer(MinTimer, _questionTimerPanel, true, new[]
            {
                new TimerThreshold(3, () => { SoundManager.Instance.PlayNoAnswer(); }),
                new TimerThreshold(0, Close)
            });


            if (_plan.IsCatInPoke)
            {
                _catInPokePlayer = null;

                _catInPokeScreen.Show(
                    _uiController.PlayerViews.Keys.ToList(),
                    _plan.CatInPoke,
                    player =>
                    {
                        Display();
                        _catInPokeScreen.Close();

                        _catInPokePlayer = player;
                        _answeringPlayer = null;

                        ShowQuestion();
                    });
            }
            else
            {
                Display();
                ShowQuestion();
            }
        }

        private void Display()
        {
            _themesGameObject.SetActive(false);

            ShowGameObject();

            _acceptButton.gameObject.SetActive(false);
            _extraPointsAcceptButton.gameObject.SetActive(false);
            _lessPointsAcceptButton.gameObject.SetActive(false);

            _declineButton.gameObject.SetActive(false);
            _canAnswerButton.gameObject.SetActive(true);
        }

        private void ShowQuestion()
        {
            _failedPlayers.Clear();

            _label.text = _plan.Question;
            _image.gameObject.SetActive(_plan.Picture != null);

            if (_plan.Picture != null)
                _image.sprite = _plan.Picture;

            _videoPlayer.gameObject.SetActive(_plan.Video != null);

            if (_plan.Video != null)
                _videoPlayer.Show(_plan.Video);

            _answeringPlayerLabel.text = "";
            _answeringPlayer = null;

            _timerCounter.Show();
            _timerCounter.RunTimer(_questionTimer);

            _uiController.PlayerAnswering += HandlePlayerAnswering;
            _uiController.QuestionReader.Say("<rate speed='3'>" + _plan.Question +"</rate>");

            if (string.IsNullOrEmpty(_plan.Question))
                CanAnswerHandler();
        }

        private void Update()
        {
            if (_canAcceptAnswers)
                return;

            if (string.IsNullOrEmpty(_plan.Question))
                return;

            if (_uiController.QuestionReader.Status(0) % 2 != 0)
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

            _playerTimer = new QuizTimer(_plan.IsCatInPoke ? PlayerAnswerTimer * 2 : PlayerAnswerTimer,
                _playerTimerPanel, false, new[]
                {
                    new TimerThreshold(0, IncorrectAnswer),
                });

            _timerCounter.RunTimer(_playerTimer);

            _acceptButton.gameObject.SetActive(true);
            _extraPointsAcceptButton.gameObject.SetActive(true);
            _lessPointsAcceptButton.gameObject.SetActive(true);

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

            if (_plan.Audio != null)
                _audioSource.PlayOneShot(_plan.Audio);

            _canAcceptAnswers = true;

            if (_plan.IsCatInPoke && _catInPokePlayer != null)
                HandlePlayerAnswering(_catInPokePlayer);
        }

        private void CorrectAnswer(float multiplier = 1f)
        {
            var resultPoints = Mathf.RoundToInt(QuestionPrice * multiplier);

            SetAsAnswering(_answeringPlayer, false);

            _answeringPlayer.UpdatePoints(resultPoints);
            _uiController.SetDecisionMaker(_answeringPlayer);

            _questionTimer.Unpause();
            _audioSource.UnPause();

            _answeringPlayer.SendMessage(new QuizCommand
            {
                Command = SocketServer.CorrectAnswer,
                Parameter = resultPoints.ToString(CultureInfo.InvariantCulture)
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
            _extraPointsAcceptButton.gameObject.SetActive(false);
            _lessPointsAcceptButton.gameObject.SetActive(false);

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