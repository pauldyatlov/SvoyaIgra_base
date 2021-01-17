using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Quiz.Gameplay.UI
{
    public class StreamVideo : MonoBehaviour
    {
        [SerializeField] private RawImage _image = default;

        private VideoClip _videoClip;

        private VideoPlayer _videoPlayer;
        private AudioSource _audioSource;

        private void Awake()
        {
            _videoPlayer = gameObject.AddComponent<VideoPlayer>();
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        public void Show(VideoClip videoClip)
        {
            _videoClip = videoClip;

            StartCoroutine(Co_PlayView());
        }

        private IEnumerator Co_PlayView()
        {
            _videoPlayer.playOnAwake = false;
            _audioSource.playOnAwake = false;
            _audioSource.Pause();

            _videoPlayer.source = VideoSource.VideoClip;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

            _videoPlayer.EnableAudioTrack(0, true);
            _videoPlayer.SetTargetAudioSource(0, _audioSource);

            _videoPlayer.clip = _videoClip;
            _videoPlayer.Prepare();

            var waitTime = new WaitForSeconds(.5f);
            while (!_videoPlayer.isPrepared)
            {
                yield return waitTime;
                break;
            }

            _image.texture = _videoPlayer.texture;

            _videoPlayer.Play();
            _audioSource.Play();

            while (_videoPlayer.isPlaying)
                yield return null;
        }

        public void SetPauseStatus(bool value)
        {
            if (_videoPlayer == null)
                return;

            if (value)
                _videoPlayer.Pause();
            else
                _videoPlayer.Play();
        }
    }
}