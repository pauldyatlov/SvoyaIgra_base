﻿using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TaskScreen : MonoBehaviour
{
    [SerializeField] private GameObject _themesGameObject;
    [SerializeField] private CatInPokeScreen _catInPokeScreen;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private StreamVideo _videoPlayer;

    [SerializeField] private Text _answeringPlayerLabel;
    [SerializeField] private Text _timerLabel;
    [SerializeField] private Text _label;
    [SerializeField] private Image _image;

    [SerializeField] private Button _acceptButton;
    [SerializeField] private Button _declineButton;

    [SerializeField] private Button _canAnswerButton;

    private readonly List<Player> _failedPlayers = new List<Player>();
    private bool _paused;

    private QuestionsGameplayPlan _gameplayPlan;
    private Player _answeringPlayer;

    private const int MinTimer = 15;
    private bool _canAcceptAnswers;
    private Coroutine _timeCoroutine;
    private Player _catInPokePlayer;

    private int QuestionPrice =>
        _gameplayPlan.CatInPoke.Price > 0 ? _gameplayPlan.CatInPoke.Price : _gameplayPlan.Price;

    private void Awake()
    {
        _acceptButton.onClick.AddListener(CorrectAnswer);
        _declineButton.onClick.AddListener(IncorrectAnswer);

        _canAnswerButton.onClick.AddListener(CanAnswerHandler);
    }

    public void Show(QuestionsGameplayPlan plan)
    {
        _canAcceptAnswers = false;
        _gameplayPlan = plan;

        if (plan.IsCatInPoke)
        {
            _catInPokePlayer = null;

            _catInPokeScreen.Show(plan.CatInPoke, Engine.RegisteredPlayers.ToList(), player =>
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
        gameObject.SetActive(true);

        _acceptButton.gameObject.SetActive(false);
        _declineButton.gameObject.SetActive(false);
        _canAnswerButton.gameObject.SetActive(true);
    }

    private void ShowQuestion(QuestionsGameplayPlan plan)
    {
        _failedPlayers.Clear();

        _label.text = _gameplayPlan.Question;

        _image.gameObject.SetActive(plan.Picture != null);

        if (plan.Picture != null)
            _image.sprite = plan.Picture;

        _videoPlayer.gameObject.SetActive(_gameplayPlan.Video != null);

        if (_gameplayPlan.Video != null)
            _videoPlayer.Show(_gameplayPlan.Video);

        _answeringPlayerLabel.text = "";
        _answeringPlayer = null;

        _timerLabel.text = MinTimer.ToString(CultureInfo.InvariantCulture);

        Engine.OnPlayerAnswering += HandlePlayerAnswering;

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

        _timeCoroutine = StartCoroutine(Co_GameplayRoundTimer(_gameplayPlan.IsCatInPoke ? 1 : MinTimer));

        _canAnswerButton.gameObject.SetActive(false);

        if (_gameplayPlan.Audio != null)
            _audioSource.PlayOneShot(_gameplayPlan.Audio);

        _canAcceptAnswers = true;

        if (_gameplayPlan.IsCatInPoke && _catInPokePlayer != null)
            HandlePlayerAnswering(_catInPokePlayer);
    }

    private void CorrectAnswer()
    {
        SetAsAnswering(_answeringPlayer, false);
        _answeringPlayer.UpdatePoints(QuestionPrice);

        Time.timeScale = 1.0f;

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

        foreach (var view in Engine.PlayerViews.Where(x => !_failedPlayers.Contains(x.Key)))
            view.Value.SetCanvasGroup(!value);

        var registeredPlayer = Engine.PlayerViews.FirstOrDefault(x => x.Key == player);

        if (registeredPlayer.Value != null)
            registeredPlayer.Value.SetCanvasGroup(value);
    }

    private IEnumerator Co_GameplayRoundTimer(float time)
    {
        while (true)
        {
            for (var i = 0; i < time; i++)
            {
                _timerLabel.text = (time - i).ToString(CultureInfo.InvariantCulture);

                yield return new WaitForSeconds(1);
            }

            Close();
        }
    }

    public void Close()
    {
        if (_answeringPlayer != null)
            IncorrectAnswer();

        foreach (var view in Engine.PlayerViews)
            view.Value.SetCanvasGroup(true);

        Engine.OnPlayerAnswering -= HandlePlayerAnswering;

        _themesGameObject.SetActive(true);
        gameObject.SetActive(false);
    }
}