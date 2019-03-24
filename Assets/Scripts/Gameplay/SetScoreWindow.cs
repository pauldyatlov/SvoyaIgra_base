using System;
using UnityEngine;
using UnityEngine.UI;

public class SetScoreWindow : MonoBehaviour
{
    [SerializeField] private Text _label;
    [SerializeField] private InputField _inputField;

    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _setButton;

    private Action<string> _onScoreSet;
    private string _scoreToSet;

    private void Awake()
    {
        _inputField.onEndEdit.AddListener(arg => { _scoreToSet = arg; });

        _closeButton.onClick.AddListener(() => { gameObject.SetActive(false); });
        _setButton.onClick.AddListener(() =>
        {
            _onScoreSet?.Invoke(_scoreToSet);

            gameObject.SetActive(false);
        });
    }

    public void Show(Player player, Action<string> onScoreSet)
    {
        _label.text = "Установим очки для " + player.Name + "!";
        _onScoreSet = onScoreSet;
        _inputField.text = player.Points.ToString();

        gameObject.SetActive(true);
    }
}