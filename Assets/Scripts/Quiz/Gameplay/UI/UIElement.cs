using UnityEngine;
using UnityEngine.EventSystems;

namespace Quiz.Gameplay.UI
{
    public class UIElement : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public void ShowGameObject()
        {
            gameObject.SetActive(true);
        }

        public void HideGameObject()
        {
            gameObject.SetActive(false);
        }

        public virtual void Close()
        {
            HideGameObject();
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {

        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {

        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {

        }
    }
}