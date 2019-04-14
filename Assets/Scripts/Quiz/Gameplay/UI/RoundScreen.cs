using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Quiz.Gameplay.UI
{
    public class RoundScreen : UIElement
    {
        [SerializeField] private ThemePanel _themePanel = default;
        [SerializeField] private RectTransform _themeContainer = default;
        [SerializeField] private Button _startRoundButton = default;

        private readonly Dictionary<ThemePlan, ThemePanel> _themes = new Dictionary<ThemePlan, ThemePanel>();

        private RoundPlan _plan;
        private Action<QuestionPlan> _onQuestionSelected;
        private Action<RoundPlan> _onThemesEnded;
        private UIController _uiController;

        private void Awake()
        {
            _startRoundButton.onClick.AddListener(() =>
            {
                BeginRound();

                _startRoundButton.gameObject.SetActive(false);
            });
        }

        public void Show(int index, UIController controller, RoundPlan plan, Action<QuestionPlan> onQuestionSelected,
            Action<RoundPlan> onThemesEnded)
        {
            _uiController = controller;
            _plan = plan;
            _onQuestionSelected = onQuestionSelected;
            _onThemesEnded = onThemesEnded;

            if (index > 0)
            {
                _startRoundButton.gameObject.SetActive(true);
            }
            else
            {
                _startRoundButton.gameObject.SetActive(false);
                BeginRound();
            }
        }

        private void BeginRound()
        {
            SoundManager.Instance.PlayRoundBegin();

            foreach (var theme in _plan.ThemesList)
            {
                var themePanel = Instantiate(_themePanel, _themeContainer);
                themePanel.Show(theme, _onQuestionSelected, RemoveTheme);

                _themes.Add(theme, themePanel);
            }

            _uiController.SetDecisionMaker();
        }

        private void RemoveTheme(ThemePlan theme)
        {
            _themes[theme].Close();

            _themes.Remove(theme);

            if (_themes.Count <= 0)
                _onThemesEnded.Invoke(_plan);
        }
    }
}