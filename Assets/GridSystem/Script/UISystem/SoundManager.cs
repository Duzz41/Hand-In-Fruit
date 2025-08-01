using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // SceneManager için gerekli

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance; // Singleton instance

    public List<AudioSource> musicSources = new List<AudioSource>();
    public List<AudioSource> sfxSources = new List<AudioSource>();

    void Awake()
    {
        // Eğer instance zaten varsa, bu objeyi yok et
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Singleton instance'ını ayarla
        instance = this;

        // Sahnedeki tüm AudioSource bileşenlerini bul
        FindAllAudioSources();

        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // Sahne yüklendiğinde dinleyiciyi kaldır
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Yeni sahne yüklendiğinde çağrılır
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAllAudioSources();
    }

    // Tüm AudioSource bileşenlerini bul ve gruplandır
    public void FindAllAudioSources()
    {
        // Öncelikle mevcut listeleri temizle
        musicSources.Clear();
        sfxSources.Clear();

        // Sahnedeki tüm AudioSource bileşenlerini bul
        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

        foreach (AudioSource source in allAudioSources)
        {
            // Eğer AudioSource'un bulunduğu objenin tag'i "Music" ise, müzik olarak kabul et
            if (source.CompareTag("Music"))
            {
                musicSources.Add(source);
                source.volume = PlayerPrefs.GetFloat("MusicVolume");
            }
            else
            {
                // Diğer tüm kaynakları SFX olarak kabul et
                sfxSources.Add(source);
                source.volume = PlayerPrefs.GetFloat("SFXVolume");
            }
        }
    }

    // Müzik sesini aç/kapat
    public void SetMusicVolume(float volume)
    {
        foreach (AudioSource musicSource in musicSources)
        {
            musicSource.volume = volume;
        }
    }

    // SFX sesini aç/kapat
    public void SetSFXVolume(float volume)
    {
        foreach (AudioSource sfxSource in sfxSources)
        {
            sfxSource.volume = volume;
        }
    }

    // Yeni bir AudioSource eklendiğinde çağrılacak metot
    public void RegisterAudioSource(AudioSource newSource)
    {
        if (newSource.CompareTag("Music"))
        {
            musicSources.Add(newSource);
            newSource.volume = PlayerPrefs.GetFloat("MusicVolume");
        }
        else
        {
            sfxSources.Add(newSource);
            newSource.volume = PlayerPrefs.GetFloat("SFXVolume");
        }
    }

    public void UnregisterAudioSource(AudioSource sourceToRemove)
    {
        if (sourceToRemove.CompareTag("Music"))
        {
            if (musicSources.Contains(sourceToRemove))
            {
                musicSources.Remove(sourceToRemove);
            }
        }
        else
        {
            if (sfxSources.Contains(sourceToRemove))
            {
                sfxSources.Remove(sourceToRemove);
            }
        }
    }
}
