using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Quiz.Gameplay.UI
{
    public class QuestionPanel : UIElement
    {
        [SerializeField] private GameObject _background;

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _price;

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
            _background.SetActive(true);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            _background.SetActive(false);
        }

        public void Close()
        {
            _canvasGroup.SetStatus(false);
        }
    }
}