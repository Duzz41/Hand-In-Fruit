using TMPro; // TextMeshPro UI bile�enleri i�in gerekli
using UnityEngine;

public class SellAreaHandler : MonoBehaviour
{
    // === UI Element Referanslar� ===
    [Header("UI Element References")]
    [Tooltip("Sat�� pop-up'�n�n ana GameObject'i (Panel).")]
    public GameObject sellPopupPanel;

    [Tooltip("Pop-up �zerindeki ba�l�k metni (�rn: 'Madenler Sat�ld�!').")]
    public TMP_Text titleText;

    [Tooltip("Pop-up �zerindeki kazan�lan para miktar�n� g�steren metin.")]
    public TMP_Text moneyEarnedText;

    [Tooltip("Pop-up �zerindeki arac�n delme g�c� seviyesini g�steren metin.")]
    public TMP_Text drillPowerText;

    [Tooltip("Pop-up �zerindeki arac�n X-Ray g�r�� �zelli�inin durumunu g�steren metin.")]
    public TMP_Text xRayVisionText;

    [Tooltip("Pop-up'� kapatma butonunun metin bile�eni (varsa).")]
    public TMP_Text closeButtonText;

    // === Sat�� Alan� Ayarlar� ===
    [Header("Sell Area Settings")]
    [Tooltip("Sat�� alan�n� tetikleyecek oyuncu arac�n�n GameObject Tag'i.")]
    public string playerVehicleTag = "PlayerVehicle";

    // === Pop-up Metin Formatlar� ve Varsay�lan De�erler ===
    [Header("Pop-up Display Text Formatting")]
    [Tooltip("Kazan�lan para metni i�in format dizesi. {0} kazan�lan paray� temsil eder.")]
    public string moneyFormatString = "Kazan�lan Para: {0} Alt�n";

    [Tooltip("Delme g�c� metni i�in format dizesi. {0} delme g�c� seviyesini temsil eder.")]
    public string drillPowerFormatString = "Delme G�c�: Seviye {0}";

    [Tooltip(
        "X-Ray g�r�� metni i�in format dizesi. {0} X-Ray durumunu ('Aktif'/'Deaktif') temsil eder."
    )]
    public string xRayVisionFormatString = "X-Ray G�r��: {0}";

    [Tooltip("Pop-up'�n ba�l�k metni.")]
    public string popupTitleString = "Madenler Sat�ld�!";

    [Tooltip("Kapatma butonunun varsay�lan metni.")]
    public string closeButtonDefaultText = "Tamam";

    void Start()
    {
        // UpgradeManager instance'�n�n mevcut olup olmad���n� kontrol et
        // Bu script, UpgradeManager'dan sonra �al��t���ndan emin olmak i�in �nemlidir.
        // Genellikle Script Execution Order ayarlar�ndan kontrol edilir.
        if (UpgradeManager.Instance == null)
        {
            Debug.LogError(
                "UpgradeManager.Instance bulunamad�! L�tfen sahnede bir 'UpgradeManager' GameObject'i oldu�undan ve 'UpgradeManager.cs' script'inin ona eklendi�inden emin olun. Ayr�ca Script Execution Order ayarlar�n�z� kontrol edin.",
                this
            );
            // Hata durumunda script'i devre d��� b�rakmak faydal� olabilir
            // enabled = false;
            return; // Metottan ��k
        }

        // Oyun ba�lad���nda sat�� pop-up panelini gizle
        if (sellPopupPanel != null)
        {
            sellPopupPanel.SetActive(false);
        }

        // Kapatma butonunun metnini ayarla (e�er bir metin bile�eni atanm��sa)
        if (closeButtonText != null)
        {
            closeButtonText.text = closeButtonDefaultText;
        }
    }

    /// <summary>
    /// Bir ba�ka Collider bu tetikleyiciye girdi�inde �a�r�l�r.
    /// </summary>
    /// <param name="other">Tetikleyiciye giren Collider bile�eni.</param>
    void OnTriggerEnter(Collider other)
    {
        // Giren objenin belirlenen "playerVehicleTag" etiketine sahip olup olmad���n� kontrol et
        // ve UpgradeManager instance'�n�n var oldu�undan emin ol.
        if (other.CompareTag(playerVehicleTag) && UpgradeManager.Instance != null)
        {
            Debug.Log(
                $"Oyuncu Arac� ({other.gameObject.name}) Sat�� Alan�na Girdi. Madenler Sat�l�yor...",
                this
            );

            // UpgradeManager �zerinden toplanm�� madenleri sat ve kazan�lan paray� al
            int earnedMoney = UpgradeManager.Instance.SellAllMines();

            // Pop-up'taki ba�l�k metnini ayarla
            if (titleText != null)
            {
                titleText.text = popupTitleString;
            }

            // Kazan�lan para metnini formatlayarak g�ncelle
            if (moneyEarnedText != null)
            {
                moneyEarnedText.text = string.Format(moneyFormatString, earnedMoney);
            }

            // Arac�n g�ncel delme g�c� seviyesini formatlayarak g�ncelle
            if (drillPowerText != null)
            {
                drillPowerText.text = string.Format(
                    drillPowerFormatString,
                    UpgradeManager.Instance.drillPowerLevel
                );
            }

            // Arac�n X-Ray g�r�� durumunu formatlayarak g�ncelle
            if (xRayVisionText != null)
            {
                // X-Ray durumu i�in "Aktif" veya "Deaktif" metnini belirle
                string xRayStatus = UpgradeManager.Instance.hasXRayVision ? "Aktif" : "Deaktif";
                xRayVisionText.text = string.Format(xRayVisionFormatString, xRayStatus);
            }

            // Sat�� pop-up panelini aktif hale getirerek g�ster
            if (sellPopupPanel != null)
            {
                sellPopupPanel.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Sat�� pop-up'�n� kapatmak i�in buton taraf�ndan �a�r�l�r.
    /// </summary>
    public void CloseSellPopup()
    {
        // Sat�� pop-up panelini deaktif hale getirerek gizle
        if (sellPopupPanel != null)
        {
            sellPopupPanel.SetActive(false);
            Debug.Log("Sat�� Pop-up'� kapat�ld�.");
        }
    }
}
