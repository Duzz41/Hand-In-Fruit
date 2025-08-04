using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class UpgradeTrigger : MonoBehaviour
{
    [Tooltip("Bu tetikleyici alan� aktif edecek GameObject'in Tag'i (genellikle 'Player').")]
    public string playerTag = "Player";

    [Header("Unity Events")]
    [Tooltip("Oyuncu trigger alan�na girdi�inde �a�r�lacak event'ler.")]
    public UnityEvent onPlayerEnter;

    [Tooltip("Oyuncu trigger alan�ndan ��kt���nda �a�r�lacak event'ler.")]
    public UnityEvent onPlayerExit;

    [Header("Upgrade Screen Settings")]
    [Tooltip("Oyuncu alan�na girdi�inde upgrade ekran�n� otomatik a�.")]
    public bool autoOpenUpgradeScreen = true;

    [Tooltip("Oyuncu alan�ndan ��kt���nda upgrade ekran�n� otomatik kapat.")]
    public bool autoCloseUpgradeScreen = true;

    [Header("Optional UI Feedback")]
    [Tooltip("Oyuncuya g�sterilecek bilgi mesaj� (iste�e ba�l�).")]
    public TMP_Text infoText;

    [Tooltip("Oyuncu alan�nda iken g�sterilecek mesaj.")]
    public string enterMessage = "Y�kseltme Alan� - E tu�una bas�n";

    [Tooltip("Oyuncu alan�ndan ��karken g�sterilecek mesaj.")]
    public string exitMessage = "";

    [Header("Key Input Settings")]
    [Tooltip("Upgrade ekran�n� a�mak i�in kullan�lacak tu�.")]
    public KeyCode upgradeKey = KeyCode.E;

    [Tooltip("Tu� ile upgrade ekran� kontrol� aktif mi?")]
    public bool useKeyInput = true;

    // Private de�i�kenler
    private bool playerInArea = false;
    private Collider triggerCollider;

    void Start()
    {
        // Collider referans�n� al ve ayarlar�n� kontrol et
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            Debug.LogError("UpgradeTrigger: Collider bile�eni bulunamad�!", this);
            return;
        }
        // Collider'�n trigger olup olmad���n� kontrol et ve gerekirse d�zelt
        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning(
                "UpgradeTrigger: Collider 'Is Trigger' olarak ayarlanmal�! Otomatik d�zeltiliyor.",
                this
            );
            triggerCollider.isTrigger = true;
        }
        // Info text'i ba�lang��ta gizle
        if (infoText != null)
        {
            infoText.gameObject.SetActive(false);
        }
        // Event'ler null ise ba�lat
        if (onPlayerEnter == null)
            onPlayerEnter = new UnityEvent();
        if (onPlayerExit == null)
            onPlayerExit = new UnityEvent();
    }

    void Update()
    {
        // Oyuncu alandaysa ve tu� kontrol� aktifse
        if (playerInArea && useKeyInput && Input.GetKeyDown(upgradeKey))
        {
            ToggleUpgradeScreen();
        }
    }

    void OnValidate()
    {
        // Editor'da Collider ayarlar�n� kontrol et
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning(
                "UpgradeTrigger script'ini kullanan objenin Collider'� 'Is Trigger' olarak ayarlanmal�.",
                this
            );
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"Oyuncu ({other.gameObject.name}) y�kseltme alan�na girdi.", this);
            playerInArea = true;
            // Info mesaj�n� g�ster
            ShowInfoMessage(enterMessage);
            // Otomatik upgrade ekran� a�ma
            if (autoOpenUpgradeScreen && UIManager.Instance != null)
            {
                UIManager.Instance.OpenUpgradeScreen();
            }
            // Unity Event'ini tetikle
            onPlayerEnter.Invoke();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"Oyuncu ({other.gameObject.name}) y�kseltme alan�ndan ��kt�.", this);
            playerInArea = false;
            // Info mesaj�n� gizle veya ��k�� mesaj�n� g�ster
            if (!string.IsNullOrEmpty(exitMessage))
            {
                ShowInfoMessage(exitMessage);
                // 1 saniye sonra mesaj� gizle
                Invoke(nameof(HideInfoMessage), 1f);
            }
            else
            {
                HideInfoMessage();
            }
            // Otomatik upgrade ekran� kapatma
            if (autoCloseUpgradeScreen && UIManager.Instance != null)
            {
                UIManager.Instance.CloseUpgradeScreen();
            }
            // Unity Event'ini tetikle
            onPlayerExit.Invoke();
        }
    }

    /// <summary>
    /// Upgrade ekran�n� a�/kapat i�lemi yapar.
    /// </summary>
    private void ToggleUpgradeScreen()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance bulunamad�! Upgrade ekran� a��lam�yor.");
            return;
        }
        // Upgrade ekran�n�n �u anki durumunu kontrol et
        bool isUpgradeScreenActive =
            UIManager.Instance.upgradeScreenPanel != null
            && UIManager.Instance.upgradeScreenPanel.activeInHierarchy;
        if (isUpgradeScreenActive)
        {
            UIManager.Instance.CloseUpgradeScreen();
            Debug.Log("Upgrade ekran� kapat�ld�.");
        }
        else
        {
            UIManager.Instance.OpenUpgradeScreen();
            Debug.Log("Upgrade ekran� a��ld�.");
        }
    }

    /// <summary>
    /// Bilgi mesaj�n� g�sterir.
    /// </summary>
    /// <param name="message">G�sterilecek mesaj</param>
    private void ShowInfoMessage(string message)
    {
        if (infoText != null && !string.IsNullOrEmpty(message))
        {
            infoText.text = message;
            infoText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Bilgi mesaj�n� gizler.
    /// </summary>
    private void HideInfoMessage()
    {
        if (infoText != null)
        {
            infoText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Public metot: D��ardan upgrade ekran�n� a�mak i�in kullan�labilir.
    /// </summary>
    public void OpenUpgradeScreen()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenUpgradeScreen();
        }
    }

    /// <summary>
    /// Public metot: D��ardan upgrade ekran�n� kapatmak i�in kullan�labilir.
    /// </summary>
    public void CloseUpgradeScreen()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseUpgradeScreen();
        }
    }

    /// <summary>
    /// Public metot: Oyuncunun �u anda trigger alan�nda olup olmad���n� d�nd�r�r.
    /// </summary>
    /// <returns>Oyuncu alandaysa true, de�ilse false</returns>
    public bool IsPlayerInArea()
    {
        return playerInArea;
    }
}
