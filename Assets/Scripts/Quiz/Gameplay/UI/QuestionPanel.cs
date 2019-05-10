using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Quiz.Gameplay.UI
{
    public class QuestionPanel : UIElement
    {
        [SerializeField] private Image _background = default;
        [SerializeField] private Sprite _defaultSprite = default;
        [SerializeField] private Sprite _hoverSprite = default;

        [SerializeField] private CanvasGroup _canvasGroup = default;
        [SerializeField] private TextMeshProUGUI _price = default;

        private QuestionPlan _plan;
        private Action<QuestionPlan> _onQuestionSelected;

        public void Show(QuestionPlan plan, Action<QuestionPlan> onQuestionSelected)
        {
            _plan = plan;
            _onQuestionSelected = onQuestionSelected;

            ShowGameObject();

            _price.text = plan.Price.ToString();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            _onQuestionSelected?.Invoke(_plan);
            _canvasGroup.SetStatus(false);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            _background.sprite = _hoverSprite;
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            _background.sprite = _defaultSprite;
        }

        public override void Close()
        {
            _canvasGroup.SetStatus(false);
        }
    }
}