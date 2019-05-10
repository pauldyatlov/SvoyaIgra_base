using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Quiz.Gameplay.UI
{
    public class FinalRoundTheme : UIElement
    {
        [SerializeField] private TextMeshProUGUI _roundName = default;

        private FinalQuestion _question;
        private Action<FinalQuestion> _onClick;

        public void Show(FinalQuestion question, Action<FinalQuestion> onClick)
        {
            ShowGameObject();

            _question = question;
            _onClick = onClick;

            _roundName.text = question.Theme;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            _onClick?.Invoke(_question);
        }
    }
}