using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Quiz.Gameplay.UI
{
    public class GameInfoWindow : UIElement
    {
        [SerializeField] private TextMeshProUGUI _roomName = default;
        [SerializeField] private Button _okButton = default;

        private void Awake()
        {
            _okButton.onClick.AddListener(Close);
        }

        public void Show(string room)
        {
            ShowGameObject();

            _roomName.text = room;
        }
    }
}