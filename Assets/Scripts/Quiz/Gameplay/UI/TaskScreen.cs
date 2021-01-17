using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Quiz.Network;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quiz.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Quiz.Gameplay.UI
{
    public class TaskScreen : UIElement
    {
        [SerializeField] private GameObject _themesGameObject = default;
        [SerializeField] private StreamVideo _videoPlayer = default;
        [SerializeField] private AudioSource _audioSource = default;

        [SerializeField] private AudioClip[] _applauseClips = default;
        [SerializeField] private AudioSource _applauseSource = default;

        [SerializeField] private TimerPanel _questionTimerPanel = default;
        [SerializeField] private TimerPanel _playerTimerPanel = default;
        [SerializeField] private TimerCounter _timerCounter = default;

        [SerializeField] private CatInPokeScreen _catInPokeScreen = default;
        [SerializeField] private TextMeshProUGUI _answeringPlayerLabel = default;
        [SerializeField] private GameObject _answeringPlayerPanel = default;

        [SerializeField] private DecideAnswerWindow _decideAnswerWindow = default;
        [SerializeField] private TextMeshProUGUI _questionText = default;
        [SerializeField] private Image _questionImage = default;

        private readonly List<Player> _failedPlayers = new List<Player>();
        private bool _paused;

        private QuestionPlan _plan;
        private Player _answeringPlayer;

        private const int MinTimer = 12;
        private const int PlayerAnswerTimer = 18;

        private bool _canAcceptAnswers;
        private Player _catInPokePlayer;
        private UIController _uiController;

        private int QuestionPrice => _plan.CatInPoke.Price > 0 ? _plan.CatInPoke.Price : _plan.Price;
        private readonly List<Player> _countedPlayers = new List<Player>();

        private QuizTimer _questionTimer;
        private QuizTimer _playerTimer;
        private Coroutine _autoTypeText;
        private bool _wasAnswerCorrect;

        private void Awake()
        {
            _decideAnswerWindow.Show(arg =>
            {
                _wasAnswerCorrect = arg > 0;

                if (_wasAnswerCorrect)
                    CorrectAnswer(arg);
                else
                    IncorrectAnswer();
            });
        }

        public void Show(QuestionPlan plan, UIController controller)
        {
            _canAcceptAnswers = false;

            _plan = plan;
            _uiController = controller;

            _questionTimer = new QuizTimer(MinTimer, _questionTimerPanel, true, new[]
            {
                new TimerThreshold(3, () => { SoundManager.Instance.PlayNoAnswer(); }),
                new TimerThreshold(0, () =>
                {
                    _wasAnswerCorrect = false;

                    Close();
                })
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

            _decideAnswerWindow.HideGameObject();
        }

        private void ShowQuestion()
        {
            _failedPlayers.Clear();

            if (_autoTypeText != null)
                StopCoroutine(_autoTypeText);

            _autoTypeText = StartCoroutine(Co_AutoTypeText(_plan.Question));

            _questionImage.gameObject.SetActive(_plan.Picture != null);

            if (_plan.Picture != null)
            {
                _questionImage.sprite = _plan.Picture;

                _questionImage.DisableSpriteOptimizations();
                _questionImage.OnRebuildRequested();
            }

            _videoPlayer.gameObject.SetActive(_plan.Video != null);

            if (_plan.Video != null)
                _videoPlayer.Show(_plan.Video);

            _answeringPlayerPanel.SetActive(false);
            _answeringPlayerLabel.text = "";
            _answeringPlayer = null;

            _timerCounter.Show();
            _timerCounter.RunTimer(_questionTimer);

            _uiController.PlayerAnswering += HandlePlayerAnswering;
            _uiController.QuestionReader.Say("<rate speed='3'>" + _plan.Question + "</rate>");

            if (string.IsNullOrEmpty(_plan.Question))
                CanAnswerHandler();
        }

        private void Update()
        {
            if (_canAcceptAnswers)
                return;

            if (Input.GetKeyDown(KeyCode.Space))
                CanAnswerHandler();

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

            _decideAnswerWindow.ShowGameObject();

            _questionTimer.Pause();
            _audioSource.Pause();
            _videoPlayer.SetPauseStatus(true);

            _answeringPlayer = player;
            SetAsAnswering(_answeringPlayer, true);
        }

        private void CanAnswerHandler()
        {
            _questionTimer.Unpause();

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
            _videoPlayer.SetPauseStatus(false);

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
            _videoPlayer.SetPauseStatus(false);

            _decideAnswerWindow.HideGameObject();

            _answeringPlayer.SendMessage(new QuizCommand
            {
                Command = SocketServer.WrongAnswer,
                Parameter = (-QuestionPrice).ToString()
            });

            _playerTimer?.Stop();
            _playerTimer = null;

            _answeringPlayer = null;
        }

        private IEnumerator Co_AutoTypeText(string textToType)
        {
            _questionText.text = string.Empty;

            foreach (var unused in textToType)
                _questionText.text += "  ";

            var resultString = new StringBuilder(_questionText.text);

            for (var i = 0; i < textToType.Length; i++)
            {
                resultString[i] = textToType[i];

                _questionText.text = resultString.ToString();
                yield return new WaitForSeconds(.05f);
            }
        }

        private void SetAsAnswering(Player player, bool value)
        {
            _answeringPlayerPanel.SetActive(value);
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

        private async Task ShowAnswer()
        {
            _questionTimer.Pause();

            await Task.Yield();

            var answerString = _plan.Answer.Text;
            var answerPicture = _plan.Answer.Picture;
            var answerVideo = _plan.Answer.Video;

            _questionImage.gameObject.SetActive(answerPicture != null);

            if (string.IsNullOrEmpty(answerString) == false)
            {
                _questionText.text = answerString;

                await new WaitForSeconds(2f);
            }

            if (answerPicture != null)
            {
                _questionImage.sprite = answerPicture;

                _questionImage.DisableSpriteOptimizations();
                _questionImage.OnRebuildRequested();

                await new WaitForSeconds(2f);
            }

            _videoPlayer.gameObject.SetActive(answerVideo);

            if (answerVideo)
            {
                _videoPlayer.Show(answerVideo);
                await new WaitForSeconds((float)answerVideo.length);
            }

            if (_wasAnswerCorrect)
            {
                var applauseClip = _applauseClips.PickRandom();
                _applauseSource.PlayOneShot(applauseClip);
            }
        }

        public override void Close()
        {
            AsyncClose().HandleExceptions();
        }

        private async Task AsyncClose()
        {
            ClearCounter();
            _countedPlayers.Clear();

            if (_answeringPlayer != null)
                IncorrectAnswer();
            else
                await ShowAnswer();

            foreach (var view in _uiController.PlayerViews)
                view.Value.SetCanvasGroup(true);

            _uiController.PlayerAnswering -= HandlePlayerAnswering;

            _themesGameObject.SetActive(true);
            _timerCounter.Close();

            _playerTimer?.Stop();
            _playerTimer = null;

            if (_autoTypeText != null)
                StopCoroutine(_autoTypeText);

            _autoTypeText = null;

            _decideAnswerWindow.Close();

            base.Close();
        }
    }
}