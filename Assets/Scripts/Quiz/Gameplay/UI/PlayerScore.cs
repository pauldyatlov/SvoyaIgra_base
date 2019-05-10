using System;
using System.Collections;
using Quiz.Network;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Quiz.Gameplay.UI
{
    public class PlayerScore : UIElement
    {
        [SerializeField] private GameObject _decisionMakerMarker = default;
        [SerializeField] private Image _background = default;

        [SerializeField] private TextMeshProUGUI _nameLabel = default;
        [SerializeField] private TextMeshProUGUI _pointsLabel = default;

        [SerializeField] private Color _defaultBackgroundColor = default;
        [SerializeField] private Color _blinkBackgroundColor = default;
        [SerializeField] private Color _offlineBackgroundColor = default;

        [SerializeField] private Color _defaultLabelColor = default;
        [SerializeField] private Color _offlineLabelColor = default;

        [SerializeField] private TextMeshProUGUI _latencyLabel = default;
        [SerializeField] private CanvasGroup _canvasGroup = default;
        [SerializeField] private Button _closeButton = default;

        private event Action<Player> OnPlayerSelected;
        private event Action<Player> OnPlayerKicked;

        private Player _player;

        private bool _countdown;
        private float _latency;

        private void Awake()
        {
            _closeButton.onClick.AddListener(() => { OnPlayerKicked?.Invoke(_player); });
        }

        public void Show(Player player, Action<Player> onPlayerSelected, Action<Player> onPlayerKicked)
        {
            _player = player;

            OnPlayerSelected = onPlayerSelected;
            OnPlayerKicked = onPlayerKicked;

            _nameLabel.text = _player.Name;
            _pointsLabel.text = _player.Points.ToString();

            _player.OnPointsUpdateAction += arg =>
            {
                _nameLabel.text = arg.Name.ToString();

                if (_pointsLabel != null)
                    _pointsLabel.text = arg.Points.ToString();
            };

            _player.OnNameChanged += playerName => { _nameLabel.text = playerName; };
            _player.OnButtonPressed += () =>
            {
                if (this == null)
                    return;

                StartCoroutine(Co_ChangeColorTemporary(0.1f, _defaultBackgroundColor, _blinkBackgroundColor,
                    () =>
                    {
                        if (this == null)
                            return;

                        StartCoroutine(Co_ChangeColorTemporary(0.1f, _blinkBackgroundColor, _defaultBackgroundColor,
                            () => { }));
                    }));
            };

            _player.OnlineStatusChanged += SetConnectedStatus;

            _player.OnSetAsDecisionMaker += arg =>
            {
                if (_decisionMakerMarker != null)
                    _decisionMakerMarker.SetActive(arg);
            };
        }

        public void SetCanvasGroup(bool value)
        {
            if (!_player.Online)
            {
                _canvasGroup.alpha = 0.3f;
                return;
            }

            _canvasGroup.alpha = value ? 1 : 0.3f;
        }

        public void SetConnectedStatus(bool online)
        {
            SetCanvasGroup(online);

            _background.color = online ? _defaultBackgroundColor : _offlineBackgroundColor;

            _nameLabel.color = online ? _defaultLabelColor : _offlineLabelColor;
            _pointsLabel.color = online ? _defaultLabelColor : _offlineLabelColor;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                case PointerEventData.InputButton.Right:
                case PointerEventData.InputButton.Middle:
                {
                    OnPlayerSelected?.Invoke(_player);

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Update()
        {
            if (!_countdown)
                return;

            _latency += Time.deltaTime;

            DisplayLatency();
        }

        private void DisplayLatency()
        {
            var latency = _latency * 1000;
            var displayLatency = latency > 0 && latency < 1000;

            _latencyLabel.text = displayLatency ? latency.ToString("0") + "ms" : string.Empty;
        }

        public void SetCountdown(bool value)
        {
            _countdown = value;

            DisplayLatency();
        }

        public void LatencyLabelVisible(bool value)
        {
            if (!value)
                _latency = 0f;

            _latencyLabel.gameObject.SetActive(value);
        }

        private IEnumerator Co_ChangeColorTemporary(float duration, Color from, Color to, Action callback)
        {
            var time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                _background.color = Color.Lerp(from, to, time / duration);

                yield return null;
            }

            callback();
        }
    }
}