using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance { get; private set; }

    [SerializeField]
    private AudioSource _songAudioSource;
    [SerializeField]
    private AudioSource _uiAudioSource;

    private float _songStartOffset;

    private void Awake()
    {
        instance = this;
    }

    private void FixedUpdate()
    {
        if( PlayerPrefs.HasKey("master_volume"))
            _songAudioSource.volume = PlayerPrefs.GetFloat("master_volume");
    }

    /// <summary>
    /// Is the song audio source currently playing?
    /// </summary>
    /// <returns></returns>
    public bool GetIsPlaying()
    {
        return _songAudioSource.isPlaying;
    }

    /// <summary>
    /// Will start playing a song, replacing existing one
    /// </summary>
    /// <param name="clip"></param>
    public void PlaySong(AudioClip clip, float startingTime = 0)
    {
        if (_songAudioSource.isPlaying) StopSong();
        _songStartOffset = startingTime;
        _songAudioSource.clip = clip;
        _songAudioSource.time = startingTime;
        _songAudioSource.Play();
    }

    /// <summary>
    /// Gets the current playtime time the song is at
    /// </summary>
    /// <returns></returns>
    public float GetCurrentPlaybackTime()
    {
        return _songAudioSource.isPlaying ? _songAudioSource.time : 0f;
    }

    /// <summary>
    /// Gets the current playback time the song is at, minus the starting offset 
    /// </summary>
    /// <returns></returns>
    public float GetCurrentPlaybackTimeNormalized()
    {
        return _songAudioSource.isPlaying ? _songAudioSource.time - _songStartOffset : 0f;
    }

    /// <summary>
    /// Stops playback of the song audiosource
    /// </summary>
    public void StopSong()
    {
        _songAudioSource.Stop();
    }
}
