using UnityEngine;
using UnityEngine.Events;
using TMPro;
[RequireComponent(typeof(Collider))]
public class UpgradeTrigger : MonoBehaviour
{
    [Tooltip("Bu tetikleyici alaný aktif edecek GameObject'in Tag'i (genellikle 'Player').")]
    public string playerTag = "Player";
    [Header("Unity Events")]
    [Tooltip("Oyuncu trigger alanýna girdiðinde çaðrýlacak event'ler.")]
    public UnityEvent onPlayerEnter;
    [Tooltip("Oyuncu trigger alanýndan çýktýðýnda çaðrýlacak event'ler.")]
    public UnityEvent onPlayerExit;
    [Header("Upgrade Screen Settings")]
    [Tooltip("Oyuncu alanýna girdiðinde upgrade ekranýný otomatik aç.")]
    public bool autoOpenUpgradeScreen = true;
    [Tooltip("Oyuncu alanýndan çýktýðýnda upgrade ekranýný otomatik kapat.")]
    public bool autoCloseUpgradeScreen = true;
    [Header("Optional UI Feedback")]
    [Tooltip("Oyuncuya gösterilecek bilgi mesajý (isteðe baðlý).")]
    public TMP_Text infoText;
    [Tooltip("Oyuncu alanýnda iken gösterilecek mesaj.")]
    public string enterMessage = "Yükseltme Alaný - E tuþuna basýn";
    [Tooltip("Oyuncu alanýndan çýkarken gösterilecek mesaj.")]
    public string exitMessage = "";
    [Header("Key Input Settings")]
    [Tooltip("Upgrade ekranýný açmak için kullanýlacak tuþ.")]
    public KeyCode upgradeKey = KeyCode.E;
    [Tooltip("Tuþ ile upgrade ekraný kontrolü aktif mi?")]
    public bool useKeyInput = true;
    // Private deðiþkenler
    private bool playerInArea = false;
    private Collider triggerCollider;
    void Start()
    {
        // Collider referansýný al ve ayarlarýný kontrol et
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            Debug.LogError("UpgradeTrigger: Collider bileþeni bulunamadý!", this);
            return;
        }
        // Collider'ýn trigger olup olmadýðýný kontrol et ve gerekirse düzelt
        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning("UpgradeTrigger: Collider 'Is Trigger' olarak ayarlanmalý! Otomatik düzeltiliyor.", this);
            triggerCollider.isTrigger = true;
        }
        // Info text'i baþlangýçta gizle
        if (infoText != null)
        {
            infoText.gameObject.SetActive(false);
        }
        // Event'ler null ise baþlat
        if (onPlayerEnter == null)
            onPlayerEnter = new UnityEvent();
        if (onPlayerExit == null)
            onPlayerExit = new UnityEvent();
    }
    void Update()
    {
        // Oyuncu alandaysa ve tuþ kontrolü aktifse
        if (playerInArea && useKeyInput && Input.GetKeyDown(upgradeKey))
        {
            ToggleUpgradeScreen();
        }
    }
    void OnValidate()
    {
        // Editor'da Collider ayarlarýný kontrol et
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("UpgradeTrigger script'ini kullanan objenin Collider'ý 'Is Trigger' olarak ayarlanmalý.", this);
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"Oyuncu ({other.gameObject.name}) yükseltme alanýna girdi.", this);
            playerInArea = true;
            // Info mesajýný göster
            ShowInfoMessage(enterMessage);
            // Otomatik upgrade ekraný açma
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
            Debug.Log($"Oyuncu ({other.gameObject.name}) yükseltme alanýndan çýktý.", this);
            playerInArea = false;
            // Info mesajýný gizle veya çýkýþ mesajýný göster
            if (!string.IsNullOrEmpty(exitMessage))
            {
                ShowInfoMessage(exitMessage);
                // 1 saniye sonra mesajý gizle
                Invoke(nameof(HideInfoMessage), 1f);
            }
            else
            {
                HideInfoMessage();
            }
            // Otomatik upgrade ekraný kapatma
            if (autoCloseUpgradeScreen && UIManager.Instance != null)
            {
                UIManager.Instance.CloseUpgradeScreen();
            }
            // Unity Event'ini tetikle
            onPlayerExit.Invoke();
        }
    }
    /// <summary>
    /// Upgrade ekranýný aç/kapat iþlemi yapar.
    /// </summary>
    private void ToggleUpgradeScreen()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance bulunamadý! Upgrade ekraný açýlamýyor.");
            return;
        }
        // Upgrade ekranýnýn þu anki durumunu kontrol et
        bool isUpgradeScreenActive = UIManager.Instance.upgradeScreenPanel != null &&
                                   UIManager.Instance.upgradeScreenPanel.activeInHierarchy;
        if (isUpgradeScreenActive)
        {
            UIManager.Instance.CloseUpgradeScreen();
            Debug.Log("Upgrade ekraný kapatýldý.");
        }
        else
        {
            UIManager.Instance.OpenUpgradeScreen();
            Debug.Log("Upgrade ekraný açýldý.");
        }
    }
    /// <summary>
    /// Bilgi mesajýný gösterir.
    /// </summary>
    /// <param name="message">Gösterilecek mesaj</param>
    private void ShowInfoMessage(string message)
    {
        if (infoText != null && !string.IsNullOrEmpty(message))
        {
            infoText.text = message;
            infoText.gameObject.SetActive(true);
        }
    }
    /// <summary>
    /// Bilgi mesajýný gizler.
    /// </summary>
    private void HideInfoMessage()
    {
        if (infoText != null)
        {
            infoText.gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// Public metot: Dýþardan upgrade ekranýný açmak için kullanýlabilir.
    /// </summary>
    public void OpenUpgradeScreen()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenUpgradeScreen();
        }
    }
    /// <summary>
    /// Public metot: Dýþardan upgrade ekranýný kapatmak için kullanýlabilir.
    /// </summary>
    public void CloseUpgradeScreen()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseUpgradeScreen();
        }
    }
    /// <summary>
    /// Public metot: Oyuncunun þu anda trigger alanýnda olup olmadýðýný döndürür.
    /// </summary>
    /// <returns>Oyuncu alandaysa true, deðilse false</returns>
    public bool IsPlayerInArea()
    {
        return playerInArea;
    }
}








