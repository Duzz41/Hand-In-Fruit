using UnityEngine;
using TMPro; // TextMeshPro UI bileþenleri için gerekli

public class SellAreaHandler : MonoBehaviour
{
    // === UI Element Referanslarý ===
    [Header("UI Element References")]
    [Tooltip("Satýþ pop-up'ýnýn ana GameObject'i (Panel).")]
    public GameObject sellPopupPanel;

    [Tooltip("Pop-up üzerindeki baþlýk metni (örn: 'Madenler Satýldý!').")]
    public TMP_Text titleText;

    [Tooltip("Pop-up üzerindeki kazanýlan para miktarýný gösteren metin.")]
    public TMP_Text moneyEarnedText;

    [Tooltip("Pop-up üzerindeki aracýn delme gücü seviyesini gösteren metin.")]
    public TMP_Text drillPowerText;

    [Tooltip("Pop-up üzerindeki aracýn X-Ray görüþ özelliðinin durumunu gösteren metin.")]
    public TMP_Text xRayVisionText;

    [Tooltip("Pop-up'ý kapatma butonunun metin bileþeni (varsa).")]
    public TMP_Text closeButtonText;

    // === Satýþ Alaný Ayarlarý ===
    [Header("Sell Area Settings")]
    [Tooltip("Satýþ alanýný tetikleyecek oyuncu aracýnýn GameObject Tag'i.")]
    public string PlayerTag = "Player";

    // === Pop-up Metin Formatlarý ve Varsayýlan Deðerler ===
    [Header("Pop-up Display Text Formatting")]
    [Tooltip("Kazanýlan para metni için format dizesi. {0} kazanýlan parayý temsil eder.")]
    public string moneyFormatString = "Kazanýlan Para: {0} Altýn";

    [Tooltip("Delme gücü metni için format dizesi. {0} delme gücü seviyesini temsil eder.")]
    public string drillPowerFormatString = "Delme Gücü: Seviye {0}";

    [Tooltip("X-Ray görüþ metni için format dizesi. {0} X-Ray durumunu ('Aktif'/'Deaktif') temsil eder.")]
    public string xRayVisionFormatString = "X-Ray Görüþ: {0}";

    [Tooltip("Pop-up'ýn baþlýk metni.")]
    public string popupTitleString = "Madenler Satýldý!";

    [Tooltip("Kapatma butonunun varsayýlan metni.")]
    public string closeButtonDefaultText = "Tamam";


    void Start()
    {
        // UpgradeManager instance'ýnýn mevcut olup olmadýðýný kontrol et
        // Bu script, UpgradeManager'dan sonra çalýþtýðýndan emin olmak için önemlidir.
        // Genellikle Script Execution Order ayarlarýndan kontrol edilir.
        if (UpgradeManager.Instance == null)
        {
            Debug.LogError("UpgradeManager.Instance bulunamadý! Lütfen sahnede bir 'UpgradeManager' GameObject'i olduðundan ve 'UpgradeManager.cs' script'inin ona eklendiðinden emin olun. Ayrýca Script Execution Order ayarlarýnýzý kontrol edin.", this);
            // Hata durumunda script'i devre dýþý býrakmak faydalý olabilir
            // enabled = false; 
            return; // Metottan çýk
        }

        // Oyun baþladýðýnda satýþ pop-up panelini gizle
        if (sellPopupPanel != null)
        {
            sellPopupPanel.SetActive(false);
        }

        // Kapatma butonunun metnini ayarla (eðer bir metin bileþeni atanmýþsa)
        if (closeButtonText != null)
        {
            closeButtonText.text = closeButtonDefaultText;
        }
    }

    /// <summary>
    /// Bir baþka Collider bu tetikleyiciye girdiðinde çaðrýlýr.
    /// </summary>
    /// <param name="other">Tetikleyiciye giren Collider bileþeni.</param>
    void OnTriggerEnter(Collider other)
    {
        // Giren objenin belirlenen "PlayerTag" etiketine sahip olup olmadýðýný kontrol et
        // ve UpgradeManager instance'ýnýn var olduðundan emin ol.
        if (other.CompareTag(PlayerTag) && UpgradeManager.Instance != null)
        {
            Debug.Log($"Oyuncu Aracý ({other.gameObject.name}) Satýþ Alanýna Girdi. Madenler Satýlýyor...", this);

            // UpgradeManager üzerinden toplanmýþ madenleri sat ve kazanýlan parayý al
            int earnedMoney = UpgradeManager.Instance.SellAllMines();

            // Pop-up'taki baþlýk metnini ayarla
            if (titleText != null)
            {
                titleText.text = popupTitleString;
            }

            // Kazanýlan para metnini formatlayarak güncelle
            if (moneyEarnedText != null)
            {
                moneyEarnedText.text = string.Format(moneyFormatString, earnedMoney);
            }

            // Aracýn güncel delme gücü seviyesini formatlayarak güncelle
            if (drillPowerText != null)
            {
                drillPowerText.text = string.Format(drillPowerFormatString, UpgradeManager.Instance.drillPowerLevel);
            }

            // Aracýn X-Ray görüþ durumunu formatlayarak güncelle
            if (xRayVisionText != null)
            {
                // X-Ray durumu için "Aktif" veya "Deaktif" metnini belirle
                string xRayStatus = UpgradeManager.Instance.hasXRayVision ? "Aktif" : "Deaktif";
                xRayVisionText.text = string.Format(xRayVisionFormatString, xRayStatus);
            }

            // Satýþ pop-up panelini aktif hale getirerek göster
            if (sellPopupPanel != null)
            {
                sellPopupPanel.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Satýþ pop-up'ýný kapatmak için buton tarafýndan çaðrýlýr.
    /// </summary>
    public void CloseSellPopup()
    {
        // Satýþ pop-up panelini deaktif hale getirerek gizle
        if (sellPopupPanel != null)
        {
            sellPopupPanel.SetActive(false);
            Debug.Log("Satýþ Pop-up'ý kapatýldý.");
        }
    }
}

