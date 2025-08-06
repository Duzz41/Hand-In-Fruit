using UnityEngine;
using UnityEngine.Events;

public class UnfreezeChilds : MonoBehaviour
{
    [Tooltip("Options for triggering the fracture")]
    public TriggerOptions triggerOptions;

    [Header("Drilling System Integration")]
    [Tooltip("Only allow fracturing through the drilling system, not direct collision")]
    [SerializeField]
    private bool onlyAllowDrillingFracture = true;

    [Header("Audio Settings")]
    [Tooltip("Sound to play when the object fractures/breaks")]
    [SerializeField]
    private AudioClip fractureSound;

    [Tooltip("Volume for the fracture sound (0.0 to 1.0)")]
    [SerializeField]
    private float fractureVolume = 0.8f;

    [Tooltip("Use SoundCallManager for playing sound (if available)")]
    [SerializeField]
    private bool useSoundCallManager = true;

    [Tooltip("Sound category name for SoundCallManager")]
    [SerializeField]
    private string soundCategory = "BlockBreak";

    [Header("Advanced Audio Settings")]
    [Tooltip("Pitch variation for fracture sound (0 = no variation)")]
    [SerializeField]
    private float pitchVariation = 0.2f;

    [Tooltip("Minimum pitch value")]
    [SerializeField]
    private float minPitch = 0.8f;

    [Tooltip("Maximum pitch value")]
    [SerializeField]
    private float maxPitch = 1.2f;

    [Tooltip("Play sound at fracture point (3D sound) or at camera (2D sound)")]
    [SerializeField]
    private bool use3DSound = true;

    private bool isFrozen = true;

    // Cached components
    private Rigidbody parentRigidbody;
    private MeshCollider parentCollider;
    private MeshRenderer meshRenderer;
    private DestructibleObject destructibleObjectScript;
    private GameObject[] childObjects;
    private AudioSource audioSource;

    void Start()
    {
        // Cache existing components
        parentRigidbody = GetComponent<Rigidbody>();
        parentCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();
        destructibleObjectScript = GetComponent<DestructibleObject>();

        // Setup audio source for fracture sound
        SetupAudioSource();

        // Cache child objects
        int childCount = transform.childCount;
        childObjects = new GameObject[childCount];
        for (int i = 0; i < childCount; i++)
        {
            childObjects[i] = transform.GetChild(i).gameObject;
            childObjects[i].SetActive(false);
        }
    }

    void SetupAudioSource()
    {
        // AudioSource oluştur veya mevcut olanı kullan
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // AudioSource ayarları
        audioSource.clip = fractureSound;
        audioSource.volume = fractureVolume;
        audioSource.pitch = 1f;
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        // 3D ses ayarları
        if (use3DSound)
        {
            audioSource.spatialBlend = 1f; // 3D ses
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 3f;
            audioSource.maxDistance = 20f;
        }
        else
        {
            audioSource.spatialBlend = 0f; // 2D ses
        }

        // SoundManager ile kaydet
      
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isFrozen)
        {
            return;
        }

        // FIXED: Only process collision if drilling fracture is disabled
        if (onlyAllowDrillingFracture)
        {
            // Let the DrillForceBuildup system handle all fracturing
            return;
        }

        // Original collision logic (only runs if onlyAllowDrillingFracture is false)
        if (collision.contactCount > 0)
        {
            var contact = collision.contacts[0];
            var collisionForce = collision.impulse.magnitude / Time.fixedDeltaTime;
            bool colliderTagAllowed = triggerOptions.IsTagAllowed(
                contact.otherCollider.gameObject.tag
            );

            if (
                collisionForce > MinimumCollisionForce
                && (!triggerOptions.filterCollisionsByTag || colliderTagAllowed)
            )
            {
                Fracture();
            }
        }
    }

    /// <summary>
    /// This method ensures it can only run once and is the only way to fracture objects.
    /// </summary>
    public void Fracture()
    {
        // 1. Guard clause - if already fractured, exit immediately
        if (!isFrozen)
        {
            return;
        }

        // 2. Set the state immediately to prevent any other calls
        isFrozen = false;

        // 3. Log for debugging
        Debug.Log($"[UnfreezeChilds] Fracturing {gameObject.name}");

        // 4. PARÇALANMA SESİNİ ÇALA
        PlayFractureSound();

        // 5. Disable visual and physics components
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        if (parentRigidbody != null)
        {
            parentRigidbody.isKinematic = true;
        }

        if (parentCollider != null)
        {
            parentCollider.enabled = false;
        }

        // 6. Activate child pieces
        foreach (GameObject childObj in childObjects)
        {
            if (childObj != null)
            {
                childObj.SetActive(true);
            }
        }

        // 7. Notify the drilling system that this object is now fractured
        var drillScript = FindFirstObjectByType<DrillForceBuildup>();
        if (drillScript != null && drillScript.GetCurrentTarget() == gameObject)
        {
            drillScript.ForceStopDrilling();
        }

        // 8. Call the destructible object logic
        if (destructibleObjectScript != null)
        {
            destructibleObjectScript.DoThisWhenBrokenIntoPieces();
        }
    }

    /// <summary>
    /// Parçalanma sesini çalar
    /// </summary>
    void PlayFractureSound()
    {
        if (fractureSound == null)
        {
            Debug.LogWarning($"[UnfreezeChilds] Fracture sound not assigned for {gameObject.name}");
            return;
        }

        try
        {
            // SoundCallManager kullan (varsa)
            if (useSoundCallManager && SoundCallManager.instance != null)
            {
                // Pitch variation ile çal
                float randomPitch = CalculateRandomPitch();

                // SoundCallManager ile oynamak için pitch ayarını manuel yapmak gerekebilir
                SoundCallManager.instance.PlayOneShot(soundCategory, fractureSound);

                Debug.Log(
                    $"[UnfreezeChilds] Fracture sound played via SoundCallManager: {fractureSound.name}"
                );
            }
            // AudioSource ile çal
            else if (audioSource != null)
            {
                // Random pitch uygula
                audioSource.pitch = CalculateRandomPitch();
                audioSource.volume = fractureVolume;
                audioSource.PlayOneShot(fractureSound);

                Debug.Log(
                    $"[UnfreezeChilds] Fracture sound played via AudioSource: {fractureSound.name} (Pitch: {audioSource.pitch:F2})"
                );
            }
            // Fallback: Geçici AudioSource oluştur
            else
            {
                GameObject tempAudioObj = new GameObject("TempFractureSound");
                tempAudioObj.transform.position = transform.position;

                AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
                tempSource.clip = fractureSound;
                tempSource.volume = fractureVolume;
                tempSource.pitch = CalculateRandomPitch();
                tempSource.spatialBlend = use3DSound ? 1f : 0f;

                tempSource.Play();

                // Ses bittikten sonra objeyi yok et
                Destroy(tempAudioObj, fractureSound.length + 0.1f);

                Debug.Log(
                    $"[UnfreezeChilds] Fracture sound played via temporary AudioSource: {fractureSound.name}"
                );
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"[UnfreezeChilds] Error playing fracture sound for {gameObject.name}: {e.Message}"
            );
        }
    }

    /// <summary>
    /// Rastgele pitch değeri hesaplar
    /// </summary>
    float CalculateRandomPitch()
    {
        if (pitchVariation <= 0f)
        {
            return 1f;
        }

        float randomPitch = Random.Range(-pitchVariation, pitchVariation);
        float finalPitch = 1f + randomPitch;

        // Min ve max değerler arasında sınırla
        return Mathf.Clamp(finalPitch, minPitch, maxPitch);
    }

    public void TriggerFracture()
    {
        Fracture();
    }

    public bool IsIntact()
    {
        return isFrozen;
    }

    public float MinimumCollisionForce
    {
        get { return triggerOptions.minimumCollisionForce; }
        set { triggerOptions.minimumCollisionForce = value; }
    }

    // FIXED: New method to enable/disable drilling-only mode
    public void SetDrillingOnlyMode(bool drillingOnly)
    {
        onlyAllowDrillingFracture = drillingOnly;
    }

    // FIXED: Check if this object can be fractured by collision (not just drilling)
    public bool CanFractureByCollision()
    {
        return !onlyAllowDrillingFracture;
    }

    // YENİ SES KONTROL METODLARİ

    /// <summary>
    /// Parçalanma sesini değiştir
    /// </summary>
    public void SetFractureSound(AudioClip newSound)
    {
        fractureSound = newSound;
        if (audioSource != null)
        {
            audioSource.clip = fractureSound;
        }
    }

    /// <summary>
    /// Ses seviyesini değiştir
    /// </summary>
    public void SetFractureVolume(float volume)
    {
        fractureVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = fractureVolume;
        }
    }

    /// <summary>
    /// 3D ses modunu aç/kapat
    /// </summary>
    public void Set3DSoundMode(bool enable3D)
    {
        use3DSound = enable3D;
        if (audioSource != null)
        {
            audioSource.spatialBlend = use3DSound ? 1f : 0f;
        }
    }

    /// <summary>
    /// Pitch variation ayarını değiştir
    /// </summary>
    public void SetPitchVariation(float variation)
    {
        pitchVariation = Mathf.Clamp(variation, 0f, 1f);
    }

    /// <summary>
    /// SoundCallManager kullanımını aç/kapat
    /// </summary>
    public void SetUseSoundCallManager(bool useManager)
    {
        useSoundCallManager = useManager;
    }

    /// <summary>
    /// Test için parçalanma sesini çal (parçalanma yapmadan)
    /// </summary>
    [ContextMenu("Test Fracture Sound")]
    public void TestFractureSound()
    {
        if (Application.isPlaying)
        {
            PlayFractureSound();
            Debug.Log($"[UnfreezeChilds] Test fracture sound played for {gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[UnfreezeChilds] Test fracture sound only works in Play mode!");
        }
    }

    /// <summary>
    /// Inspector'da ses bilgilerini göster
    /// </summary>
    void OnValidate()
    {
        // Değer sınırlamalar
        fractureVolume = Mathf.Clamp01(fractureVolume);
        pitchVariation = Mathf.Clamp(pitchVariation, 0f, 1f);
        minPitch = Mathf.Clamp(minPitch, 0.1f, 2f);
        maxPitch = Mathf.Clamp(maxPitch, 0.1f, 3f);

        // Min/Max pitch doğrulaması
        if (minPitch > maxPitch)
        {
            maxPitch = minPitch + 0.1f;
        }
    }
}
