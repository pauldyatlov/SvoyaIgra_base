using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FinalRoundTheme : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Text _roundName;

    private FinalQuestion _question;
    private Action<FinalQuestion> _onClick;

    public void Show(FinalQuestion question, Action<FinalQuestion> onClick)
    {
        _question = question;
        _onClick = onClick;

        gameObject.SetActive(true);

        _roundName.text = question.Theme;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _onClick?.Invoke(_question);
    }
}
