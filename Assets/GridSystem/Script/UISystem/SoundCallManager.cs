using System.Collections.Generic;
using UnityEngine;

public class SoundCallManager : MonoBehaviour
{
    public static SoundCallManager instance;

    private Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterAudioSource(AudioSource audioSource, string key)
    {
        if (!audioSources.ContainsKey(key))
        {
            audioSources[key] = audioSource;
        }
    }

    public void PlaySound(string key)
    {
        if (audioSources.ContainsKey(key))
        {
            audioSources[key].Play();
        }
        else
        {
            Debug.LogWarning($"Sound with key {key} not found!");
        }
    }

    public void StopSound(string key)
    {
        if (audioSources.ContainsKey(key))
        {
            audioSources[key].Stop();
        }
    }

    public void PlayOneShot(string key, AudioClip clip)
    {
        if (audioSources.ContainsKey(key))
        {
            audioSources[key].PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"Sound with key {key} not found!");
        }
    }
}
