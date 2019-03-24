using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameQuestion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject _background;
    [SerializeField] private CanvasGroup _canvasGroup;

    [SerializeField] private Button _questionButton;
    [SerializeField] private Text _priceLabel;

    public void Init(string price, Action onQuestionPressed)
    {
        _priceLabel.text = price;

        _questionButton.onClick.AddListener(() => onQuestionPressed());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _background.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _background.SetActive(false);
    }

    public void Close()
    {
        _canvasGroup.alpha = 0;
        _canvasGroup.interactable = false;
    }
}