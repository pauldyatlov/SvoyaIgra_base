using UnityEngine;

namespace Quiz.Gameplay
{
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;

        [SerializeField] private AudioClip _roundBegin;
        [SerializeField] private AudioClip _noAnswer;

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