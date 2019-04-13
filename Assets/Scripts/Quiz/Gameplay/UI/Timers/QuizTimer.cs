using System;
using Quiz.Gameplay.UI;

namespace Quiz.Gameplay
{
    public class QuizThreshold
    {
        public float Value;
        public Action Action;

        public QuizThreshold(float value, Action action)
        {
            Value = value;
            Action = action;
        }
    }

    public class QuizTimer
    {
        public float Duration;
        public TimerPanel Template;
        public QuizThreshold[] Thresholds;
        public bool Paused;

        public event Action<bool> OnPause;
        public event Action<QuizTimer> OnStopped;

        public QuizTimer(float duration, TimerPanel template, bool paused, QuizThreshold[] thresholds)
        {
            Duration = duration;
            Template = template;
            Thresholds = thresholds;
            Paused = paused;
        }

        public void Pause()
        {
            Paused = true;
            OnPause?.Invoke(Paused);
        }

        public void Unpause()
        {
            Paused = false;
            OnPause?.Invoke(Paused);
        }

        public void Stop()
        {
            OnStopped?.Invoke(this);
        }
    }
}