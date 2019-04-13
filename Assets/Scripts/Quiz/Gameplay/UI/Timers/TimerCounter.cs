using System.Collections.Generic;
using UnityEngine;

namespace Quiz.Gameplay.UI
{
    public class TimerCounter : UIElement
    {
        [SerializeField] private RectTransform _timersContainer = default;

        private readonly Dictionary<QuizTimer, TimerPanel> _activeTimers = new Dictionary<QuizTimer, TimerPanel>();

        public void Show()
        {
            ShowGameObject();
        }

        public void RunTimer(QuizTimer timer)
        {
            var timerPanel = Instantiate(timer.Template, _timersContainer);
            timerPanel.Show(timer);

            _activeTimers.Add(timer, timerPanel);

            timer.OnStopped += OnTimerStopped;
        }

        private void OnTimerStopped(QuizTimer timer)
        {
            _activeTimers[timer].Close();
            _activeTimers.Remove(timer);
        }

        public override void Close()
        {
            foreach (var timer in _activeTimers)
                timer.Value.Close();

            base.Close();
        }
    }
}