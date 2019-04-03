using System.Collections;
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
        [SerializeField] private CatInPokeScreen _catInPokeScreen = default;
        [SerializeField] private AudioSource _audioSource = default;
        [SerializeField] private StreamVideo _videoPlayer = default;

        [SerializeField] private Text _answeringPlayerLabel = default;
        [SerializeField] private Text _timerLabel = default;
        [SerializeField] private Text _label = default;
        [SerializeField] private Image _image = default;

        [SerializeField] private Button _acceptButton = default;
        [SerializeField] private Button _declineButton = default;

        [SerializeField] private Button _canAnswerButton = default;

        private readonly List<Player> _failedPlayers = new List<Player>();
        private bool _paused;

        private QuestionPlan _plane;
        private Player _answeringPlayer;

        private const int MinTimer = 15;
        private bool _canAcceptAnswers;
        private Coroutine _timeCoroutine;
        private Player _catInPokePlayer;
        private UIController _uiController;

        private int QuestionPrice => _plane.CatInPoke.Price > 0 ? _plane.CatInPoke.Price : _plane.Price;

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

            _timerLabel.text = MinTimer.ToString(CultureInfo.InvariantCulture);

            _uiController.PlayerAnswering += HandlePlayerAnswering;

            if (plan.Picture != null)
                CanAnswerHandler();
        }

        private void HandlePlayerAnswering(Player arg)
        {
            if (!_canAcceptAnswers || _answeringPlayer != null || _failedPlayers.Contains(arg))
                return;

            _answeringPlayer = arg;

            _acceptButton.gameObject.SetActive(true);
            _declineButton.gameObject.SetActive(true);

            Time.timeScale = 0.0f;
            _audioSource.Pause();

            SetAsAnswering(arg, true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5) && Input.GetKey(KeyCode.LeftControl))
                CanAnswerHandler();
        }

        private void CanAnswerHandler()
        {
            if (_timeCoroutine != null)
                StopCoroutine(_timeCoroutine);

            _timeCoroutine = StartCoroutine(Co_RoundTimer(_plane.IsCatInPoke ? 0.1f : MinTimer));

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

            Time.timeScale = 1.0f;
            _audioSource.UnPause();

            _answeringPlayer.SendMessage(new QuizCommand
            {
                Command = SocketServer.CorrectAnswer,
                Parameter = QuestionPrice.ToString()
            });

            _answeringPlayer = null;

            Close();
        }

        private void IncorrectAnswer()
        {
            _failedPlayers.Add(_answeringPlayer);

            SetAsAnswering(_answeringPlayer, false);
            _answeringPlayer.UpdatePoints(-QuestionPrice);

            Time.timeScale = 1.0f;
            _audioSource.UnPause();

            _acceptButton.gameObject.SetActive(false);
            _declineButton.gameObject.SetActive(false);

            _answeringPlayer.SendMessage(new QuizCommand
            {
                Command = SocketServer.WrongAnswer,
                Parameter = (-QuestionPrice).ToString()
            });

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

        private IEnumerator Co_RoundTimer(float time)
        {
            while (true)
            {
                for (var i = 0; i < time; i++)
                {
                    _timerLabel.text = time >= 1
                        ? (time - i).ToString(CultureInfo.InvariantCulture)
                        : string.Empty;

                    yield return new WaitForSeconds(1);

                    if (i == 10)
                        SoundManager.Instance.PlayNoAnswer();
                }

                Close();
            }
        }

        private void Close()
        {
            if (_answeringPlayer != null)
                IncorrectAnswer();

            foreach (var view in _uiController.PlayerViews)
                view.Value.SetCanvasGroup(true);

            _uiController.PlayerAnswering -= HandlePlayerAnswering;

            _themesGameObject.SetActive(true);

            HideGameObject();
        }
    }
}