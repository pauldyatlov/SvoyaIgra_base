using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Quiz.Gameplay.UI
{
    public class FinalRoundScreen : UIElement
    {
        [SerializeField] private TextMeshProUGUI _finalQuestion = default;

        [SerializeField] private FinalRoundTheme _roundTheme = default;
        [SerializeField] private RectTransform _roundsContainer = default;

        private readonly List<FinalQuestion> _finalThemes = new List<FinalQuestion>();

        public void Show(IEnumerable<FinalQuestion> questions)
        {
            ShowGameObject();

            foreach (var question in questions)
            {
                _finalThemes.Add(question);

                var questionPanel = Instantiate(_roundTheme, _roundsContainer);
                questionPanel.Show(question, arg =>
                {
                    questionPanel.gameObject.SetActive(false);
                    _finalThemes.Remove(arg);

                    if (_finalThemes.Count > 0)
                        return;

                    _finalQuestion.gameObject.SetActive(true);
                    _finalQuestion.text = arg.Question;
                });
            }
        }
    }
}