using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameTheme : MonoBehaviour
{
    [SerializeField] private GameQuestion _gameQuestionTemplate;
    [SerializeField] private Text _themeNameLabel;
    [SerializeField] private RectTransform _questionsContainer;

    [SerializeField] private TaskScreen _taskScreen;

    private readonly List<GameQuestion> _availableQuestions = new List<GameQuestion>();

    public Action<GameTheme, int> _onAvailableQuestionsEnd;

    public void Init(RectTransform placeholder, List<QuestionsGameplayPlan> questions, string themeName, int round)
    {
        gameObject.SetActive(true);

        transform.SetParent(placeholder, false);

        _themeNameLabel.text = themeName;

        for (var i = 0; i < questions.Count; i++)
        {
            var index = i;
            var instantiatedQuestion = Instantiate(_gameQuestionTemplate, _questionsContainer, false);

            _availableQuestions.Add(instantiatedQuestion);
            instantiatedQuestion.Init(questions[index].Price.ToString(), () =>
            {
                _availableQuestions.Remove(instantiatedQuestion);

                if (_availableQuestions.Count <= 0)
                    _onAvailableQuestionsEnd?.Invoke(this, round);

                instantiatedQuestion.Close();

                _taskScreen.Show(questions[index]);
            });
        }
    }
}