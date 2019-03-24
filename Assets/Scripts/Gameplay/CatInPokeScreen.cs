using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CatInPokeScreen : MonoBehaviour
{
    [SerializeField] private Text _label;

    [SerializeField] private RectTransform _container;
    [SerializeField] private GamePlayerStats _playerStatsTemplate;

    private readonly List<GamePlayerStats> _playersList = new List<GamePlayerStats>();

    private Action<Player> _callback;

    public void Show(CatInPokeQuestion question, List<Player> players, Action<Player> callback)
    {
        gameObject.SetActive(true);

        _callback = callback;

        _label.text = "\'" + question.Theme + "\', " + question.Price;

        foreach (var player in players)
        {
            var stat = Instantiate(_playerStatsTemplate, _container, false);
            stat.Init(player, _callback, null);

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