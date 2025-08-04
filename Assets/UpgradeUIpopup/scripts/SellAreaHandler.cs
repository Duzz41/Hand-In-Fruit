using TMPro;
using UnityEngine;

// Bu script'i sarı Sell plane'e ekleyin.
// Collider'ı Is Trigger olarak işaretlemeyi unutmayın.
[RequireComponent(typeof(Collider))]
public class SellAreaHandler : MonoBehaviour
{
    [Tooltip("Bu tetikleyiciyi aktif edecek GameObject'in Tag'i (genellikle 'Player').")]
    public string playerTag = "Player";

    [Header("Optional UI Feedback")]
    [Tooltip("Satış işlemi sırasında gösterilecek mesaj (isteğe bağlı).")]
    public TMP_Text sellFeedbackText;

    [Tooltip("Feedback mesajının ne kadar süre gösterileceği (saniye).")]
    public float feedbackDisplayTime = 2f;
    private Coroutine feedbackCoroutine;

    void Start()
    {
        // Collider'ın trigger olup olmadığını kontrol et
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("SellAreaHandler: Collider 'Is Trigger' olarak ayarlanmalı!", this);
            col.isTrigger = true; // Otomatik düzelt
        }
        // Feedback text'i başlangıçta gizle
        if (sellFeedbackText != null)
        {
            sellFeedbackText.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Giren objenin etiketini ve gerekli manager'ların varlığını kontrol et
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"Oyuncu Aracı ({other.gameObject.name}) Satış Alanına Girdi.", this);
            // UpgradeManager kontrolü
            if (UpgradeManager.Instance == null)
            {
                Debug.LogError("UpgradeManager.Instance bulunamadı! Satış işlemi yapılamıyor.");
                return;
            }
            // InventoryManager kontrolü
            if (InventoryManager.Instance == null)
            {
                Debug.LogError("InventoryManager.Instance bulunamadı! Envanter temizlenemeyecek.");
                return;
            }
            // Satış işlemini gerçekleştir
            PerformSellOperation();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"Oyuncu Aracı ({other.gameObject.name}) Satış Alanından Çıktı.", this);
            // Feedback mesajını gizle
            if (sellFeedbackText != null && sellFeedbackText.gameObject.activeInHierarchy)
            {
                if (feedbackCoroutine != null)
                {
                    StopCoroutine(feedbackCoroutine);
                }
                sellFeedbackText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Satış işlemini gerçekleştirir ve gerekli UI güncellemelerini yapar.
    /// </summary>
    private void PerformSellOperation()
    {
        try
        {
            // Madenler satılmadan önce envanterdeki maden sayısını kontrol et
            int stoneCount = InventoryManager.Instance.GetResourceCount("Stone");
            int ironCount = InventoryManager.Instance.GetResourceCount("Iron");
            int goldCount = InventoryManager.Instance.GetResourceCount("Gold");
            int totalMines = stoneCount + ironCount + goldCount;
            if (totalMines <= 0)
            {
                Debug.Log("Satılacak maden yok!");
                ShowSellFeedback("Satılacak maden yok!");
                return;
            }
            // Satış işlemini gerçekleştir
            int earnedMoney = UpgradeManager.Instance.SellAllMines();
            // Envanteri temizle
            InventoryManager.Instance.ClearInventory();
            Debug.Log($"Satış tamamlandı! Kazanılan para: {earnedMoney}");
            // UI feedback göster
            ShowSellFeedback($"Satış Tamamlandı!\n+{earnedMoney} Para Kazandınız!");
            // UIManager varsa sell popup'ını göster
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenSellPopup();
                // 2 saniye sonra popup'ı otomatik kapat
                Invoke(nameof(CloseSellPopupDelayed), 2f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Satış işlemi sırasında hata oluştu: {e.Message}");
        }
    }

    /// <summary>
    /// Satış feedback mesajını gösterir.
    /// </summary>
    /// <param name="message">Gösterilecek mesaj</param>
    private void ShowSellFeedback(string message)
    {
        if (sellFeedbackText != null)
        {
            sellFeedbackText.text = message;
            sellFeedbackText.gameObject.SetActive(true);
            // Önceki coroutine'i durdur
            if (feedbackCoroutine != null)
            {
                StopCoroutine(feedbackCoroutine);
            }
            // Belirli süre sonra mesajı gizle
            feedbackCoroutine = StartCoroutine(HideFeedbackAfterDelay());
        }
    }

    /// <summary>
    /// Belirli süre sonra feedback mesajını gizler.
    /// </summary>
    private System.Collections.IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDisplayTime);
        if (sellFeedbackText != null)
        {
            sellFeedbackText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Sell popup'ını gecikmeli kapatır (Invoke ile çağrılır).
    /// </summary>
    private void CloseSellPopupDelayed()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseSellPopup();
        }
    }
}
