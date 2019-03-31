using System;
using System.Collections.Generic;
using UnityEngine;

namespace Quiz.Gameplay.UI
{
    public class RoundScreen : UIElement
    {
        [SerializeField] private ThemePanel _themePanel = default;
        [SerializeField] private RectTransform _themeContainer = default;

        private RoundPlan _plan;
        private readonly Dictionary<ThemePlan, ThemePanel> _themes = new Dictionary<ThemePlan, ThemePanel>();
        private Action<RoundPlan> _onThemesEnded;

        public void Show(RoundPlan plan, Action<QuestionPlan> onQuestionSelected, Action<RoundPlan> onThemesEnded)
        {
            _plan = plan;
            _onThemesEnded = onThemesEnded;

            foreach (var theme in plan.ThemesList)
            {
                var themePanel = Instantiate(_themePanel, _themeContainer);
                themePanel.Show(theme, onQuestionSelected, RemoveTheme);

                _themes.Add(theme, themePanel);
            }
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