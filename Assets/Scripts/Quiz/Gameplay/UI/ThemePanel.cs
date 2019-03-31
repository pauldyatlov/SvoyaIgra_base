using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Quiz.Gameplay.UI
{
    public class ThemePanel : UIElement
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _themeNameLabel;

        [SerializeField] private QuestionPanel _questionPanel;
        [SerializeField] private RectTransform _questionContainer;

        private ThemePlan _plan;
        private readonly Dictionary<QuestionPlan, QuestionPanel> _questions = new Dictionary<QuestionPlan, QuestionPanel>();
        private Action<ThemePlan> _onQuestionsEnded;

        public void Show(ThemePlan plan, Action<QuestionPlan> onQuestionSelected, Action<ThemePlan> onQuestionsEnded)
        {
            _plan = plan;
            _themeNameLabel.text = _plan.ThemeName;
            _onQuestionsEnded = onQuestionsEnded;

            foreach (var question in _plan.QuestionsList)
            {
                var questionPanel = Instantiate(_questionPanel, _questionContainer);
                questionPanel.Show(question, arg =>
                {
                    onQuestionSelected(arg);
                    RemoveQuestion(arg);
                });

                _questions.Add(question, questionPanel);
            }

            ShowGameObject();
        }

        private void RemoveQuestion(QuestionPlan question)
        {
            _questions[question].Close();

            _questions.Remove(question);

            if (_questions.Count <= 0)
                _onQuestionsEnded.Invoke(_plan);
        }

        public void Close()
        {
            _canvasGroup.SetStatus(false);
        }
    }
}