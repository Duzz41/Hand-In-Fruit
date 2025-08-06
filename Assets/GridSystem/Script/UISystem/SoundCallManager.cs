using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Sound
{
    [Header("Sound Settings")]
    public string soundName;
    public AudioClip audioClip;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Pitch Settings")]
    [Range(0.1f, 3f)]
    public float pitch = 1f;

    [Header("Pitch Variation")]
    [Range(0f, 0.5f)]
    public float pitchVariation = 0f;

    [Header("Loop Settings")]
    public bool loop = false;

    [Header("3D Sound Settings")]
    [Range(0f, 1f)]
    public float spatialBlend = 0f; // 0 = 2D, 1 = 3D

    [Header("3D Distance Settings")]
    public float minDistance = 1f;
    public float maxDistance = 500f;

    [Header("Audio Source")]
    [HideInInspector]
    public AudioSource source;

    // Random pitch hesaplama
    public float GetRandomPitch()
    {
        if (pitchVariation <= 0f)
            return pitch;

        float randomVariation = Random.Range(-pitchVariation, pitchVariation);
        return pitch + randomVariation;
    }
}

public class SoundCallManager : MonoBehaviour
{
    public static SoundCallManager instance;

    [Header("Sound Library")]
    [SerializeField]
    private Sound[] sounds;

    [Header("Settings")]
    [SerializeField]
    private bool initializeOnAwake = true;

    [SerializeField]
    private bool showDebugLogs = true;

    [Header("Master Volume")]
    [Range(0f, 1f)]
    [SerializeField]
    private float masterVolume = 1f;

    private Dictionary<string, Sound> soundDictionary = new Dictionary<string, Sound>();

    void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            if (initializeOnAwake)
            {
                InitializeSounds();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (!initializeOnAwake)
        {
            InitializeSounds();
        }
    }

    /// <summary>
    /// Tüm sesleri başlat ve AudioSource'ları oluştur
    /// </summary>
    public void InitializeSounds()
    {
        soundDictionary.Clear();

        foreach (Sound sound in sounds)
        {
            if (sound.audioClip == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning(
                        $"[SoundCallManager] AudioClip is missing for sound: {sound.soundName}"
                    );
                continue;
            }

            if (string.IsNullOrEmpty(sound.soundName))
            {
                if (showDebugLogs)
                    Debug.LogWarning(
                        $"[SoundCallManager] Sound name is empty for clip: {sound.audioClip.name}"
                    );
                continue;
            }

            // AudioSource oluştur
            GameObject soundObject = new GameObject($"Sound_{sound.soundName}");
            soundObject.transform.SetParent(transform);

            sound.source = soundObject.AddComponent<AudioSource>();
            SetupAudioSource(sound);

            // Dictionary'e ekle
            if (!soundDictionary.ContainsKey(sound.soundName))
            {
                soundDictionary[sound.soundName] = sound;
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[SoundCallManager] Duplicate sound name: {sound.soundName}");
            }
        }

        if (showDebugLogs)
            Debug.Log($"[SoundCallManager] Initialized {soundDictionary.Count} sounds");
    }

    /// <summary>
    /// AudioSource ayarlarını yapar
    /// </summary>
    void SetupAudioSource(Sound sound)
    {
        sound.source.clip = sound.audioClip;
        sound.source.volume = sound.volume * masterVolume;
        sound.source.pitch = sound.pitch;
        sound.source.loop = sound.loop;
        sound.source.spatialBlend = sound.spatialBlend;
        sound.source.minDistance = sound.minDistance;
        sound.source.maxDistance = sound.maxDistance;
        sound.source.playOnAwake = false;
    }

    /// <summary>
    /// Ses çalar (loop sesleri için)
    /// </summary>
    public void PlaySound(string soundName)
    {
        Sound sound = GetSound(soundName);
        if (sound != null && sound.source != null)
        {
            if (!sound.source.isPlaying)
            {
                sound.source.pitch = sound.GetRandomPitch();
                sound.source.volume = sound.volume * masterVolume;
                sound.source.Play();

                if (showDebugLogs)
                    Debug.Log($"[SoundCallManager] Playing sound: {soundName}");
            }
        }
    }

    /// <summary>
    /// Sesi durdurur
    /// </summary>
    public void StopSound(string soundName)
    {
        Sound sound = GetSound(soundName);
        if (sound != null && sound.source != null)
        {
            sound.source.Stop();

            if (showDebugLogs)
                Debug.Log($"[SoundCallManager] Stopped sound: {soundName}");
        }
    }

    /// <summary>
    /// Tek seferlik ses çalar (SFX için)
    /// </summary>
    public void PlayOneShot(string soundName)
    {
        Sound sound = GetSound(soundName);
        if (sound != null && sound.source != null)
        {
            sound.source.pitch = sound.GetRandomPitch();
            sound.source.volume = sound.volume * masterVolume;
            sound.source.PlayOneShot(sound.audioClip);

            if (showDebugLogs)
                Debug.Log($"[SoundCallManager] Playing one-shot: {soundName}");
        }
    }

    /// <summary>
    /// Belirli bir AudioClip ile PlayOneShot (eski sistem uyumluluğu için)
    /// </summary>
    public void PlayOneShot(string soundName, AudioClip clip)
    {
        Sound sound = GetSound(soundName);
        if (sound != null && sound.source != null)
        {
            sound.source.pitch = sound.GetRandomPitch();
            sound.source.volume = sound.volume * masterVolume;
            sound.source.PlayOneShot(clip);

            if (showDebugLogs)
                Debug.Log($"[SoundCallManager] Playing one-shot with custom clip: {soundName}");
        }
        else
        {
            // Fallback: Geçici AudioSource oluştur
            PlayOneShotFallback(clip);
        }
    }

    /// <summary>
    /// 3D pozisyonda ses çalar
    /// </summary>
    public void PlaySoundAtPosition(string soundName, Vector3 position)
    {
        Sound sound = GetSound(soundName);
        if (sound != null && sound.source != null)
        {
            sound.source.transform.position = position;
            sound.source.pitch = sound.GetRandomPitch();
            sound.source.volume = sound.volume * masterVolume;
            sound.source.PlayOneShot(sound.audioClip);

            if (showDebugLogs)
                Debug.Log($"[SoundCallManager] Playing sound at position {position}: {soundName}");
        }
    }

    /// <summary>
    /// Ses çalıyor mu kontrol eder
    /// </summary>
    public bool IsPlaying(string soundName)
    {
        Sound sound = GetSound(soundName);
        return sound != null && sound.source != null && sound.source.isPlaying;
    }

    /// <summary>
    /// Ses seviyesini değiştirir
    /// </summary>
    public void SetVolume(string soundName, float volume)
    {
        Sound sound = GetSound(soundName);
        if (sound != null)
        {
            sound.volume = Mathf.Clamp01(volume);
            if (sound.source != null)
            {
                sound.source.volume = sound.volume * masterVolume;
            }
        }
    }

    /// <summary>
    /// Master volume değiştirir
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);

        // Tüm seslerin volume'unu güncelle
        foreach (var sound in soundDictionary.Values)
        {
            if (sound.source != null)
            {
                sound.source.volume = sound.volume * masterVolume;
            }
        }

        if (showDebugLogs)
            Debug.Log($"[SoundCallManager] Master volume set to: {masterVolume}");
    }

    /// <summary>
    /// Tüm sesleri durdurur
    /// </summary>
    public void StopAllSounds()
    {
        foreach (var sound in soundDictionary.Values)
        {
            if (sound.source != null && sound.source.isPlaying)
            {
                sound.source.Stop();
            }
        }

        if (showDebugLogs)
            Debug.Log("[SoundCallManager] All sounds stopped");
    }

    /// <summary>
    /// Sadece loop olmayan sesleri durdurur
    /// </summary>
    public void StopAllNonLoopingSounds()
    {
        foreach (var sound in soundDictionary.Values)
        {
            if (sound.source != null && sound.source.isPlaying && !sound.loop)
            {
                sound.source.Stop();
            }
        }
    }

    /// <summary>
    /// Belirli kategori seslerini durdurur (isim prefix'i ile)
    /// </summary>
    public void StopSoundCategory(string categoryPrefix)
    {
        foreach (var kvp in soundDictionary)
        {
            if (kvp.Key.StartsWith(categoryPrefix))
            {
                if (kvp.Value.source != null && kvp.Value.source.isPlaying)
                {
                    kvp.Value.source.Stop();
                }
            }
        }
    }

    /// <summary>
    /// Sound objesini getirir
    /// </summary>
    Sound GetSound(string soundName)
    {
        if (soundDictionary.ContainsKey(soundName))
        {
            return soundDictionary[soundName];
        }

        if (showDebugLogs)
            Debug.LogWarning($"[SoundCallManager] Sound not found: {soundName}");
        return null;
    }

    /// <summary>
    /// Fallback PlayOneShot (geçici AudioSource ile)
    /// </summary>
    void PlayOneShotFallback(AudioClip clip)
    {
        if (clip == null)
            return;

        GameObject tempObject = new GameObject("TempSound_" + clip.name);
        tempObject.transform.SetParent(transform);

        AudioSource tempSource = tempObject.AddComponent<AudioSource>();
        tempSource.clip = clip;
        tempSource.volume = masterVolume;
        tempSource.spatialBlend = 0f;
        tempSource.Play();

        // Ses bittikten sonra objeyi yok et
        Destroy(tempObject, clip.length + 0.1f);

        if (showDebugLogs)
            Debug.Log($"[SoundCallManager] Fallback one-shot played: {clip.name}");
    }

    /// <summary>
    /// Mevcut seslerin listesini döndürür
    /// </summary>
    public string[] GetAvailableSounds()
    {
        string[] soundNames = new string[soundDictionary.Count];
        soundDictionary.Keys.CopyTo(soundNames, 0);
        return soundNames;
    }

    /// <summary>
    /// Debug için ses bilgilerini yazdırır
    /// </summary>
    [ContextMenu("Debug Sound List")]
    public void DebugSoundList()
    {
        Debug.Log($"[SoundCallManager] Available sounds ({soundDictionary.Count}):");
        foreach (var kvp in soundDictionary)
        {
            var sound = kvp.Value;
            Debug.Log(
                $"- {kvp.Key}: Volume={sound.volume}, Pitch={sound.pitch}, Loop={sound.loop}, 3D={sound.spatialBlend > 0}"
            );
        }
    }

    /// <summary>
    /// Inspector'da değerler değiştiğinde çağrılır
    /// </summary>
    void OnValidate()
    {
        masterVolume = Mathf.Clamp01(masterVolume);

        // Play mode'da volume'ları güncelle
        if (Application.isPlaying && soundDictionary != null)
        {
            foreach (var sound in soundDictionary.Values)
            {
                if (sound.source != null)
                {
                    sound.source.volume = sound.volume * masterVolume;
                }
            }
        }
    }
}
