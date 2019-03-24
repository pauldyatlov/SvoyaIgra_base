using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class Engine : MonoBehaviour
{
    [SerializeField] private GameTheme _themeTemplate;
    [SerializeField] private FinalRoundTheme _finalRoundTheme;
    [SerializeField] private RectTransform _placeholder;
    [SerializeField] private GameplayPlan _gameplayPlan;

    [SerializeField] private GamePlayerStats _playerStatsTemplate;
    [SerializeField] private RectTransform _playerStatsPlaceholder;

    [SerializeField] private SetScoreWindow _setScoreWindow;
    [SerializeField] private Text _finalQuestion;

    public static event Action<Player> OnPlayerAnswering;

    public static readonly Dictionary<Player, GamePlayerStats> PlayerViews =
        new Dictionary<Player, GamePlayerStats>();

    public static readonly List<Player> RegisteredPlayers = new List<Player>();
    private readonly Dictionary<GameTheme, int> _themesGameplayPlans = new Dictionary<GameTheme, int>();
    private readonly List<FinalQuestion> _finalThemes = new List<FinalQuestion>();

    private int _currentRound;

    private void Awake()
    {
        SocketServer.Init();

        SocketServer.OnPlayerConnected += PlayerConnectedHandler;
        SocketServer.OnPlayerDisconnected += PlayerDisconnectedHandler;

        for (var i = 0; i < _gameplayPlan.RoundsList.Count; i++)
        {
            var plan = _gameplayPlan.RoundsList[i];

            foreach (var theme in plan.ThemesList)
            {
                var createdTheme = Instantiate(_themeTemplate);

                _themesGameplayPlans.Add(createdTheme, i);

                createdTheme.Init(_placeholder, theme.QuestionsList, theme.ThemeName, i);
                createdTheme._onAvailableQuestionsEnd += OnAvailableQuestionsEndHandler;

                if (i != 0)
                    createdTheme.gameObject.SetActive(false);
            }
        }

        SocketServer.OnPlayerAnswered += OnPlayerAnsweredHandler;
    }

    private void OnAvailableQuestionsEndHandler(GameTheme entity, int round)
    {
        _themesGameplayPlans.Remove(entity);

        entity.gameObject.SetActive(false);

        if (_placeholder.Cast<Transform>().Any(child => child.gameObject.activeSelf))
            return;

        _currentRound++;

        for (var i = 0; i < _themesGameplayPlans.Keys.Count; i++)
        {
            if (_themesGameplayPlans.Values.ElementAt(i) == _currentRound)
                _themesGameplayPlans.Keys.ElementAt(i).gameObject.SetActive(true);
        }

        if (_currentRound < _themesGameplayPlans.Keys.Count)
            return;

        foreach (var theme in _gameplayPlan.FinalQuestions)
        {
            _finalThemes.Add(theme);

            var themePanel = Instantiate(_finalRoundTheme, _placeholder);

            themePanel.Show(theme, arg =>
            {
                themePanel.gameObject.SetActive(false);
                _finalThemes.Remove(arg);

                if (_finalThemes.Count > 0)
                    return;

                _finalQuestion.gameObject.SetActive(true);
                _finalQuestion.text = arg.Question;
            });
        }
    }

    private void PlayerConnectedHandler(Player player)
    {
        var firstOrDefault = RegisteredPlayers.FirstOrDefault(x => x.Name == player.Name);
        if (firstOrDefault != null)
        {
            player.Points = firstOrDefault.Points;
            player.OnPointsUpdateAction = firstOrDefault.OnPointsUpdateAction;

            RegisteredPlayers.Add(player);
            RegisteredPlayers.Remove(firstOrDefault);
        }
        else
        {
            var stat = Instantiate(_playerStatsTemplate, _playerStatsPlaceholder, false);
            stat.Init(player, OnPlayerSelected, OnPlayerKicked);

            RegisteredPlayers.Add(player);
            PlayerViews.Add(player, stat);
        }

        player.OnPointsUpdateAction?.Invoke(player);
    }

    private void PlayerDisconnectedHandler(Matchmaker.Player stream)
    {
        var player = RegisteredPlayers.FirstOrDefault(x => x.Stream == stream);

        if (player == null)
        {
            Debug.LogError("Player to delete is null");
            return;
        }

        if (!PlayerViews.ContainsKey(player))
        {
            Debug.LogError("RegisteredPlayersWithViews does not contain key " + player.Name + "!");
            return;
        }

        PlayerViews[player].Close();
        PlayerViews.Remove(player);
    }

    private void OnPlayerAnsweredHandler(string nickname)
    {
        OnPlayerAnswering?.Invoke(RegisteredPlayers.FirstOrDefault(x => x.Name == nickname));
    }

    private void OnPlayerSelected(Player player)
    {
        _setScoreWindow.Show(player, score =>
        {
            int.TryParse(score, out var intScore);

            player.SetPoints(intScore);
        });
    }

    private void OnPlayerKicked(Player player)
    {
        player.Stream.Kick();
    }
}