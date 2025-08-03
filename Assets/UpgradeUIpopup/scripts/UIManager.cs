
using UnityEngine;
using TMPro;
using UnityEngine.UI; // Image ve Button bileþenleri için gerekli

public class UIManager : MonoBehaviour
{
    // === SINGLETON DESENÝ ===
    // UIManager'a oyunun her yerinden kolayca eriþim saðlar.
    public static UIManager Instance { get; private set; }

    // === UI PANEL REFERANSLARI ===
    [Header("UI Panels")]
    [Tooltip("Maden satýþý sonrasý çýkan pop-up paneli.")]
    public GameObject sellPopupPanel;
    [Tooltip("Tüm yükseltmelerin gösterildiði ana yükseltme ekraný paneli.")]
    public GameObject upgradeScreenPanel;
    [Tooltip("Delme Gücü yükseltme detay pop-up'ý.")]
    public GameObject drillUpgradePopup;
    [Tooltip("Benzin Kapasitesi yükseltme detay pop-up'ý.")]
    public GameObject fuelUpgradePopup;
    [Tooltip("X-Ray Görüþ yükseltme detay pop-up'ý.")]
    public GameObject xrayUpgradePopup;

    // === GLOBAL OYUN ÝÇÝ UI REFERANSLARI ===
    [Header("Global In-Game UI Elements")]
    [Tooltip("Oyundaki global para göstergesi TextMeshPro objesi.")]
    public TMP_Text globalMoneyText;
    [Tooltip("Benzin doldurma çubuðunun dolu kýsmý (Image bileþeni).")]
    public Image fuelBarFillImage;
    [Tooltip("Benzin miktarýný sayýsal olarak gösteren TextMeshPro objesi.")]
    public TMP_Text fuelAmountText;

    // === POP-UP ÝÇÝ METÝN VE BUTON REFERANSLARI ===
    // Bu referanslar, pop-up'larýn içeriðini dinamik olarak güncellemek için kullanýlýr.
    [Header("Drill Upgrade UI Elements")]
    public TMP_Text drillCostText;
    // Diðer pop-up'lar için benzer metin ve buton referanslarý buraya eklenebilir.

    // === KODUN BAÞLANGIÇ METOTLARI ===
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Sahne deðiþtirdiðinde bu objenin yok olmasýný engeller.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Oyun baþladýðýnda tüm panelleri gizle
        if (sellPopupPanel != null) sellPopupPanel.SetActive(false);
      //  if (upgradeScreenPanel != null) upgradeScreenPanel.SetActive(false);
        if (drillUpgradePopup != null) drillUpgradePopup.SetActive(false);
        if (fuelUpgradePopup != null) fuelUpgradePopup.SetActive(false);
        if (xrayUpgradePopup != null) xrayUpgradePopup.SetActive(false);

        // UpgradeManager script'inin referanslarýný bu script'teki UI'lara baðla
        // Bu sayede UpgradeManager, statlar deðiþtiðinde buradaki UI'larý güncelleyebilir.
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.SetUIRefs(globalMoneyText, fuelBarFillImage, fuelAmountText);

            // UI'larý baþlangýçta güncelle
            UpgradeManager.Instance.UpdateGlobalMoneyUI();
            UpgradeManager.Instance.UpdateFuelUI();
        }
        else
        {
            Debug.LogError("UpgradeManager.Instance bulunamadý. UI güncellemeleri yapýlamayacak.");
        }
    }

    // === ANA POP-UP KONTROL METOTLARI ===
    public void OpenSellPopup()
    {
        if (sellPopupPanel != null) sellPopupPanel.SetActive(true);
    }

    public void CloseSellPopup()
    {
        if (sellPopupPanel != null) sellPopupPanel.SetActive(false);
    }

    public void OpenUpgradeScreen()
    {
        if (upgradeScreenPanel != null) upgradeScreenPanel.SetActive(true);
        CloseAllDetailPopups();
    }

    public void CloseUpgradeScreen()
    {
        if (upgradeScreenPanel != null) upgradeScreenPanel.SetActive(false);
        CloseAllDetailPopups();
    }

    // === ÖZEL YÜKSELTME POP-UP KONTROL METOTLARI ===
    public void OpenDrillUpgradePopup()
    {
        CloseAllDetailPopups();
        if (drillUpgradePopup != null)
        {
            drillUpgradePopup.SetActive(true);
            // UpgradeManager'dan verileri çekip UI'ý güncelle
            if (UpgradeManager.Instance != null)
            {
                // drillCostText.text = UpgradeManager.Instance.GetDrillUpgradeCost().ToString(); // Varsayýmsal metot
            }
        }
    }

    public void CloseDrillUpgradePopup()
    {
        if (drillUpgradePopup != null) drillUpgradePopup.SetActive(false);
    }

    public void OpenFuelUpgradePopup()
    {
        CloseAllDetailPopups();
        if (fuelUpgradePopup != null) fuelUpgradePopup.SetActive(true);
    }

    public void CloseFuelUpgradePopup()
    {
        if (fuelUpgradePopup != null) fuelUpgradePopup.SetActive(false);
    }

    public void OpenXRayUpgradePopup()
    {
        CloseAllDetailPopups();
        if (xrayUpgradePopup != null) xrayUpgradePopup.SetActive(true);
    }

    public void CloseXRayUpgradePopup()
    {
        if (xrayUpgradePopup != null) xrayUpgradePopup.SetActive(false);
    }

    // === YARDIMCI METOTLAR ===
    private void CloseAllDetailPopups()
    {
        if (drillUpgradePopup != null) drillUpgradePopup.SetActive(false);
        if (fuelUpgradePopup != null) fuelUpgradePopup.SetActive(false);
        if (xrayUpgradePopup != null) xrayUpgradePopup.SetActive(false);
    }
}