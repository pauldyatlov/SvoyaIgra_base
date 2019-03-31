using System;
using System.Collections;
using Quiz.Network;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Quiz.Gameplay.UI
{
    public class PlayerScore : UIElement
    {
        [SerializeField] private Image _background = default;

        [SerializeField] private Text _nameLabel = default;
        [SerializeField] private Text _pointsLabel = default;

        [SerializeField] private Color _defaultBackgroundColor = default;
        [SerializeField] private Color _blinkBackgroundColor = default;

//        [SerializeField] private Color _defaultLabelColor = default;
        [SerializeField] private CanvasGroup _canvasGroup = default;

        [SerializeField] private Button _closeButton = default;

        private event Action<Player> OnPlayerSelected;
        private event Action<Player> OnPlayerKicked;

        private Player _player;

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
        }

        public void SetCanvasGroup(bool value)
        {
            _canvasGroup.alpha = value ? 1 : 0.3f;
        }

//        public void SetConnectedStatus(bool value)
//        {
//            _canvasGroup.SetStatus(value);
//
//            _nameLabel.color = value ? _defaultLabelColor : Color.red;
//            _pointsLabel.color = value ? _defaultLabelColor : Color.red;
//        }

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