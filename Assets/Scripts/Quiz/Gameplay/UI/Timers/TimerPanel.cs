using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using TMPro;
using UnityEngine;

namespace Quiz.Gameplay.UI
{
    public class TimerPanel : UIElement
    {
        [SerializeField] private TextMeshProUGUI _timerLabel = default;

        private readonly List<TimerThreshold> _forcedThresholds = new List<TimerThreshold>();
        private bool _canProceed;
        private QuizTimer _timer;
        private Coroutine _updateCoroutine;

        public void Show(QuizTimer timer)
        {
            _timer = timer;
            _canProceed = !timer.Paused;

            _timer.OnPause += OnPauseHandler;

            _updateCoroutine = StartCoroutine(Co_UpdateTimer());
        }

        private void OnPauseHandler(bool isPaused)
        {
            _canProceed = !isPaused;
        }

        private IEnumerator Co_UpdateTimer()
        {
            var passedTime = 0f;

            while (passedTime <= _timer.Duration)
            {
                if (_canProceed)
                {
                    passedTime += Time.deltaTime;
                    var remainingTime = _timer.Duration - passedTime;

                    UpdateTimer(remainingTime);

                    foreach (var threshold in _timer.Thresholds)
                    {
                        if (threshold.Value > remainingTime && !_forcedThresholds.Contains(threshold))
                        {
                            threshold.Action();
                            _forcedThresholds.Add(threshold);
                        }
                    }
                }

                yield return null;
            }
        }

        private void UpdateTimer(float time)
        {
            _timerLabel.text = time.ToString("0.0");
        }

        public override void Close()
        {
            if (!this)
                return;

            _timer.OnPause -= OnPauseHandler;
            StopCoroutine(_updateCoroutine);

            DestroyImmediate(gameObject);
        }
    }
}