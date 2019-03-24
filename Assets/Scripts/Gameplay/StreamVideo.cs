using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class StreamVideo : MonoBehaviour
{
    [SerializeField] private RawImage _image;

    private VideoClip _videoClip;

    private VideoPlayer _videoPlayer;
    private AudioSource _audioSource;

    public void Show(VideoClip videoClip)
    {
        _videoClip = videoClip;

        StartCoroutine(Co_PlayView());
    }

    private IEnumerator Co_PlayView()
    {
        _videoPlayer = gameObject.AddComponent<VideoPlayer>();
        _audioSource = gameObject.AddComponent<AudioSource>();

        _videoPlayer.playOnAwake = false;
        _audioSource.playOnAwake = false;
        _audioSource.Pause();

        _videoPlayer.source = VideoSource.VideoClip;
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

        _videoPlayer.EnableAudioTrack(0, true);
        _videoPlayer.SetTargetAudioSource(0, _audioSource);

        _videoPlayer.clip = _videoClip;
        _videoPlayer.Prepare();

        var waitTime = new WaitForSeconds(.1f);
        while (!_videoPlayer.isPrepared)
        {
            yield return waitTime;
            break;
        }

        _image.texture = _videoPlayer.texture;

        _videoPlayer.Play();
        _audioSource.Play();

        while (_videoPlayer.isPlaying)
        {
            yield return null;
        }
    }
}