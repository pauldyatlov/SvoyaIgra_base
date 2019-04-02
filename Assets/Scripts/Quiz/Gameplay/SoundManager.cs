using UnityEngine;

namespace Quiz.Gameplay
{
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource = default;

        [SerializeField] private AudioClip _roundBegin = default;
        [SerializeField] private AudioClip _noAnswer = default;

        public static SoundManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void PlayRoundBegin()
        {
            _audioSource.PlayOneShot(_roundBegin);
        }

        public void PlayNoAnswer()
        {
            _audioSource.PlayOneShot(_noAnswer);
        }
    }
}